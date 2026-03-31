using System;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class EnumDeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void NoConstantsEnumDeclTest()
        {
            const string src1 = "enum Color {}";
            EnumDeclNode ast1 = this.GenerateAST(src1).As<EnumDeclNode>();
            Assert.That(ast1.Identifier, Is.EqualTo("Color"));
        }

        [Test]
        public void ConstantsEnumDeclTest()
        {
            // jdk/src/java.sql/share/classes/java/sql/ClientInfoStatus.java
            const string src1 = @"
               enum ClientInfoStatus {
                /**
                 * The client info property could not be set for some unknown reason
                 * @since 1.6
                 */
                REASON_UNKNOWN,

                /**
                 * The client info property name specified was not a recognized property
                 * name.
                 * @since 1.6
                 */
                REASON_UNKNOWN_PROPERTY,

                /**
                 * The value specified for the client info property was not valid.
                 * @since 1.6
                 */
                REASON_VALUE_INVALID,

                /**
                 * The value specified for the client info property was too large.
                 * @since 1.6
                 */
                REASON_VALUE_TRUNCATED
            }
            ";
            EnumDeclNode ast1 = this.GenerateAST(src1).As<EnumDeclNode>();
            Assert.That(ast1.Identifier, Is.EqualTo("ClientInfoStatus"));
        }

        [Test]
        public void AnnotationsEnumDeclTest()
        {
            const string src1 = @"
               enum ClientInfoStatus {
                @Foo                
                REASON_UNKNOWN,

                @Bar @Bar REASON_UNKNOWN_PROPERTY,
                
                @Foo @Bar
                REASON_VALUE_INVALID
            }
            ";
            EnumDeclNode ast1 = this.GenerateAST(src1).As<EnumDeclNode>();
            Assert.That(ast1.Identifier, Is.EqualTo("ClientInfoStatus"));
        }

        [Test]
        public void EnumBodyDeclTest()
        {
            const string src1 = @"
               enum ClientInfoStatus {
                @Foo                
                REASON_UNKNOWN;

                ClientInfoStatus() {
                }
            }
            ";
            EnumDeclNode ast1 = this.GenerateAST(src1).As<EnumDeclNode>();
            Assert.That(ast1.Identifier, Is.EqualTo("ClientInfoStatus"));
        }
        
        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.enumDeclaration());
    }
}
