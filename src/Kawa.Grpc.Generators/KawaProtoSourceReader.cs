using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kawa.Grpc.Generators;

// The task runs before Grpc.Tools; Roslyn is used as a parser/type checker, not as a source generator.
internal static class KawaProtoSourceReader
{
    internal static IReadOnlyList<KawaProtoServiceDefinition> Read(ITaskItem[] files, ITaskItem[] references)
    {
        var trees = files.Where(file => File.Exists(file.ItemSpec)).Select(file =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(file.ItemSpec), path: file.ItemSpec)).ToArray();
        var supplied = references.Select(reference => reference.ItemSpec).Where(File.Exists).ToArray();
        var paths = (supplied.Any(path => Path.GetFileName(path) == "System.Runtime.dll") ? supplied
            : ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(Path.PathSeparator).Concat(supplied))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var compilation = CSharpCompilation.Create("KawaProtoDiscovery", trees,
            paths.Select(path => MetadataReference.CreateFromFile(path)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var symbols = trees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Select(node => compilation.GetSemanticModel(tree).GetDeclaredSymbol(node))).OfType<INamedTypeSymbol>()
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        var messages = new Dictionary<ITypeSymbol, KawaProtoMessageDefinition>(SymbolEqualityComparer.Default);
        var names = new Dictionary<string, ITypeSymbol>(StringComparer.Ordinal);
        var result = new List<KawaProtoServiceDefinition>();

        foreach (var implementation in symbols)
        {
            var attribute = Attribute(implementation, "Kawa.Grpc.KawaProtoServiceAttribute");
            if (attribute is null) continue;
            var service = attribute.ConstructorArguments.FirstOrDefault().Value as string;
            RequireIdentifier(service, "service");
            var contracts = implementation.AllInterfaces.Where(type => type.ContainingNamespace.ToDisplayString() == "Kawa.Abstractions"
                && type.Name is "IUseCase" or "IServerStreamingUseCase" or "IClientStreamingUseCase" or "IDuplexStreamingUseCase").ToArray();
            if (contracts.Length != 1 || implementation.IsAbstract || implementation.IsGenericType)
                Fail("KAWA5103", $"'{implementation}' must be a concrete, nongeneric class with exactly one Kawa RPC contract.");
            var contract = contracts[0];
            var operation = implementation.Name.EndsWith("UseCase", StringComparison.Ordinal) ? implementation.Name[..^7] : implementation.Name;
            RequireIdentifier(operation, "operation");
            if (result.Any(existing => existing.Name == service && existing.OperationName == operation))
                Fail("KAWA5104", $"Duplicate RPC '{service}.{operation}'.");
            var kind = contract.Name switch
            {
                "IServerStreamingUseCase" => KawaGrpcOperationKind.ServerStreaming,
                "IClientStreamingUseCase" => KawaGrpcOperationKind.ClientStreaming,
                "IDuplexStreamingUseCase" => KawaGrpcOperationKind.DuplexStreaming,
                _ => KawaGrpcOperationKind.Unary,
            };
            result.Add(new KawaProtoServiceDefinition(service!, operation, Message(contract.TypeArguments[0]), Message(contract.TypeArguments[1]), kind)
            { ImplementationType = FullName(implementation) });
        }
        return result;

        KawaProtoMessageDefinition Message(ITypeSymbol type)
        {
            if (messages.TryGetValue(type, out var cached)) return cached;
            if (type is not INamedTypeSymbol named || named.TypeKind != TypeKind.Class || named.IsGenericType || named.IsAbstract
                || !named.InstanceConstructors.Any(ctor => ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public))
                Fail("KAWA5103", $"'{type}' must be a concrete message class with a public parameterless constructor.");
            var messageName = ProtoName(type);
            var fields = new List<KawaProtoFieldDefinition>();
            var message = new KawaProtoMessageDefinition(messageName, fields) { CSharpType = FullName(type) };
            messages.Add(type, message);
            for (var current = (INamedTypeSymbol)type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            foreach (var member in current.GetMembers().Where(member => !member.IsStatic && member.DeclaredAccessibility == Accessibility.Public && !member.IsImplicitlyDeclared))
            {
                ITypeSymbol memberType;
                if (member is IPropertySymbol property && !property.IsIndexer)
                {
                    if (property.GetMethod?.DeclaredAccessibility != Accessibility.Public || property.SetMethod?.DeclaredAccessibility != Accessibility.Public)
                        Fail("KAWA5103", $"'{member}' requires public get and set/init accessors.");
                    memberType = property.Type;
                }
                else if (member is IFieldSymbol field && !field.IsConst)
                {
                    if (field.IsReadOnly) Fail("KAWA5103", $"'{member}' is readonly and cannot be populated by the adapter.");
                    memberType = field.Type;
                }
                else if (member is IPropertySymbol { IsIndexer: true })
                {
                    Fail("KAWA5103", $"'{member}' is an unsupported indexer.");
                    continue;
                }
                else continue;
                var fieldAttribute = Attribute(member, "Kawa.Grpc.KawaProtoFieldAttribute");
                if (fieldAttribute is null) Fail("KAWA5102", $"'{member}' requires an explicit KawaProtoField number.");
                var number = (int)fieldAttribute!.ConstructorArguments[0].Value!;
                if (number < 1 || number > 536870911 || number is >= 19000 and <= 19999 || fields.Any(field => field.Number == number))
                    Fail("KAWA5101", $"'{member}' has a duplicate, reserved or invalid field number {number}.");
                var repeated = false;
                var array = false;
                if (memberType is IArrayTypeSymbol arrayType && arrayType.ElementType.SpecialType != SpecialType.System_Byte)
                {
                    if (arrayType.Rank != 1) Fail("KAWA5103", $"'{member}' uses a multidimensional array.");
                    repeated = array = true;
                    memberType = arrayType.ElementType;
                }
                else if (memberType is INamedTypeSymbol collection && collection.IsGenericType && collection.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
                    && collection.Name is "List" or "IList" or "ICollection" or "IEnumerable" or "IReadOnlyList" or "IReadOnlyCollection")
                {
                    repeated = true;
                    memberType = collection.TypeArguments[0];
                }
                var scalar = Scalar(memberType);
                KawaProtoMessageDefinition? nested = null;
                IReadOnlyList<KeyValuePair<string, int>>? values = null;
                if (memberType.TypeKind == TypeKind.Enum)
                {
                    scalar = ProtoName(memberType);
                    try { values = ((INamedTypeSymbol)memberType).GetMembers().OfType<IFieldSymbol>().Where(field => field.HasConstantValue)
                        .Select(field => new KeyValuePair<string, int>(field.Name, Convert.ToInt32(field.ConstantValue, System.Globalization.CultureInfo.InvariantCulture))).ToArray(); }
                    catch (OverflowException) { Fail("KAWA5103", $"Enum '{memberType}' exceeds protobuf int32 range."); }
                }
                else if (scalar is null) { nested = Message(memberType); scalar = nested.Name; }
                fields.Add(new KawaProtoFieldDefinition(char.ToLowerInvariant(member.Name[0]) + member.Name[1..], scalar!, number)
                { CSharpName = member.Name, CSharpType = FullName(memberType), Repeated = repeated, IsArray = array, Message = nested, EnumValues = values });
            }
            return message;
        }

        string ProtoName(ITypeSymbol type)
        {
            var simple = type.ContainingType is null ? type.Name : type.ContainingType.Name + "_" + type.Name;
            if (!names.TryGetValue(simple, out var existing) || SymbolEqualityComparer.Default.Equals(existing, type))
            { names[simple] = type; return simple; }
            var qualified = FullName(type).Replace('.', '_');
            if (names.TryGetValue(qualified, out existing) && !SymbolEqualityComparer.Default.Equals(existing, type))
                Fail("KAWA5104", $"Conflicting protobuf name '{qualified}'.");
            names[qualified] = type;
            return qualified;
        }
    }

    private static string? Scalar(ITypeSymbol type) => type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte } ? "bytes" : type.SpecialType switch
    {
        SpecialType.System_String => "string", SpecialType.System_Boolean => "bool",
        SpecialType.System_Int32 => "int32", SpecialType.System_Int64 => "int64",
        SpecialType.System_UInt32 => "uint32", SpecialType.System_UInt64 => "uint64",
        SpecialType.System_Single => "float", SpecialType.System_Double => "double", _ => null,
    };
    private static string FullName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
    private static AttributeData? Attribute(ISymbol symbol, string name) => symbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == name);
    private static void RequireIdentifier(string? name, string role)
    {
        if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$"))
            Fail("KAWA5103", $"Invalid protobuf {role} name '{name}'.");
    }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string id, string message) => throw new KawaProtoSchemaException(new KawaProtoDiagnostic(id, message));
}
