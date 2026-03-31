using System;
using System.Collections.Generic;

namespace LINVAST.Builders
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ASTBuilderAttribute : Attribute
    {
        private static readonly HashSet<string> _fileExtensions = new();


        public string FileExtension { get; }


        public ASTBuilderAttribute(string fileExtension)
        {
            this.FileExtension = fileExtension;
            _fileExtensions.Add(fileExtension);
        }
    }
}
