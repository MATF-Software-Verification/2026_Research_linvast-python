using LINVAST.Imperative.Builders.Lua;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Lua
{
    internal sealed class JsonSerializationTests : JsonSerializationTestsBase
    {
        [Test]
        public void SimpleSerializationTests()
        {
            this.AssertSerialization("test = 1");
            this.AssertSerialization("local x, y = 5, 4");
            this.AssertSerialization("function f(x) return x end");
            this.AssertSerialization("if x > 5 then print(x) end");
            this.AssertSerialization("if x > 5 then print(x) elseif x > 10 then print(x+x) else print('end') end");
            this.AssertSerialization("relational = (a > b) or (a <= c) and not c ~= 3");
            this.AssertSerialization("bitwise = (x1 | a) & b << (4 >> b ~ (~0))");
            this.AssertSerialization("test = 5  -- comment");
            this.AssertSerialization("local x, y, z = 5, 2, 'a'");
        }

        [Test]
        public void ComplexSerializationTest()
        {
            this.AssertSerialization(@"
                test = 1

                a, b = 3, 4, 5
                x, y, z, a = 3, a, 5, 6, 7
                n = nil
                str = ""abc""
                t, f = true, false
                diff, type, test = 9, ""a"", true

                local x1, x2
                local x3, x4, x5 = 6, 'a'

                --arr = { 1, 2, 3, '4'}

                --dict = {}
                --dict.x = 5

                arr['a'] = 100
                arr['b'] = 200
                arr['c'] = 150

                add = a + 4
                sub = arr['a'] - 4
                mul = x1 * x2
                div = x3 / 2
                bitwise = (x1 | a) & b << (4 >> b ~ (~0))   -- ~ is `xor` but also `not`
                relational = (a > b) or (a <= c) and not c ~= 3

                strcat = ""aaaa"" + 'bbbb'

                while x < 3 do
                    inside_var = 4
                    x = x - inside_var
                end

                x = 3
                repeat
                    inside_var = 4
                    x = x - inside_var
                until x < 1

                if x > 1 then
                    print(x)
                elseif x > 10 then
                    print(n, x, y, z)
                elseif x > 20 then
                    print()
                else
                    print(diff)
                end

                function fact (n)
                  if n == 0 then
                    return 1
                  else
                    return n * fact(n-1)
                  end
                end
            ");
        }

        protected override ASTNode GenerateAST(string src)
            => new LuaASTBuilder().BuildFromSource(src);
    }
}
