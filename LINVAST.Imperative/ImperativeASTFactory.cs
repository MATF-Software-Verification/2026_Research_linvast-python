using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Nodes;

namespace LINVAST.Imperative
{
    public sealed class ImperativeASTFactory : IASTFactory
    {
        public ASTNode BuildFromFile(string path)
        {
            var fi = new FileInfo(path);
            string code = File.ReadAllText(path);

            IEnumerable<Type> builderTypes = Assembly
                .GetAssembly(typeof(ImperativeASTFactory))
                .GetExportedTypes()
                .Where(t => t.GetCustomAttributes<ASTBuilderAttribute>()
                             .Any(a => string.Equals(a.FileExtension, fi.Extension, StringComparison.InvariantCultureIgnoreCase))
                )
                ;
            if (!builderTypes.Any())
                throw new UnsupportedLanguageException();

            Type? builderType = builderTypes.SingleOrDefault();
            if (builderType is null)
                throw new AmbiguousMatchException("Unique binder not registered to handle that file type.");

            if (!(Activator.CreateInstance(builderType) is IAbstractASTBuilder builder))
                throw new NotImplementedException("The builder for required file extension is found but does not inherit IAbstractASTBuilder class.");

            return builder.BuildFromSource(code);
        }
    }
}
