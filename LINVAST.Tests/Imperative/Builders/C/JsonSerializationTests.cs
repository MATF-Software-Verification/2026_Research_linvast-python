using LINVAST.Imperative.Builders.C;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.C
{
    internal sealed class JsonSerializationTests : JsonSerializationTestsBase
    {
        [Test]
        public void SimpleSerializationTests()
        {
            this.AssertSerialization("void f() {}");
            this.AssertSerialization("int main() { return 0; }");
            this.AssertSerialization("int main(int x) { return 0; }");
            this.AssertSerialization("int main(int x, ...) { return 0; }");
            this.AssertSerialization("static extern int main(const int x, ...) { return 0; }");
            this.AssertSerialization("static const volatile bool x;");
            this.AssertSerialization("extern bool x();");
            this.AssertSerialization("bool f(); const time_t t1, t2, t3 = 2;");
            this.AssertSerialization("void f() { int x; if (1) { x = 1; } else { x = 2; } }");
            this.AssertSerialization("void f() { int x; if (1) x = 1; else x = 2; }");
            this.AssertSerialization("void f() { int x; if (1) x = 1; }");
        }

        [Test]
        public void ComplexSerializationTest()
        {
            this.AssertSerialization(@"
                extern static time_t foo_extern(int x, const int y);

                static void f(int a, const int b)
                {
                    const int x = 5, y;
                    unsigned short z = x + y;
                    signed int k = 3 + 4 * (5 - 6);
                    char* s = ""abcd"";
                    double w = 3.4 * 7.11 / 2.33;
                    unsigned int bitwise = ((1 << 10) | (1 << 5)) & (x ^ ~0);

                label:
                    x = 4;
                    goto label;
                    return a + b + x + y;
                }

                extern int foo(const float bar, int x, float y[], signed int z, unsigned long long k, ...) 
                {
                    const time_t elapsed_time = time(NULL);
                    int empty[10];
                    int a, w[3] = { 1, (2 * 3 + 4), (3 << 2) };

                    w[1] = 4;

                    bool b = 3 > 4 && 3 < 5 || 4 >= 2;
                    if (5 > 3) {
                        b = 1 != 1;
                    } else {
                        float y = 3.01;
                        y *= 3.2;
                    }

                    if (!3) {
                        bool c = !(1 != 2);
                        c = b;
                        f(3, 4);
                    }

                    if (1)
                        y = (w[1] > 5) ? 10.3 : 5.44;

                    ;

                    return a++;
                }

                static const int x = 7;
                static float y, z = 3;

                int main() 
                {
                    int x = 1;
                    while (1) {
                        if (x < 10)
                            printf(""lt 10"");
                        if (x == 17)
                            break;
                        x++;
                    }

                    for (int i = 0; i < 100; i++) {
                        foo(1, 2, 3, 4);
                        if (i > 10)
                            return 1;
                    }

                    for (x--; x < 10; x *= 2, ++i) {
                        printf(""%d\n"", x);
                    }

                    return 0;
                }"
            );
        }

        protected override ASTNode GenerateAST(string src)
            => new CASTBuilder().BuildFromSource(src);
    }
}
