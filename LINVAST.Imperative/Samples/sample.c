extern static time_t foo_extern(int x, const int y);

static void f(int a, const int b)
{
	const int x = 5, y, hex = 0xFFF, oct = 01137;
	unsigned short z = x + y;
	signed int k = 3 + 4 * (5 - 6);
	char* s = "abcd";
	double w = 3.4 * 7.11 / 2.33;
	float x = -1.3e-10f;
	int p1 = 3, *p2 = &x;
	unsigned int bitwise = ((1 << 10) | (1 << 5)) & (x ^ ~0);

label:
	x = 4;
	goto label;
	return a + b + x + y;
}

extern int foo(const float bar, int x, float y[], signed int z, unsigned long long k, ...)
{
	const time_t elapsed_time = time(NULL);
	char c1 = 'a', c2 = ' ';
	int empty[10];
	int a, w[3] = { 1, (2 * 3 + 4), (3 << 2) };

	w[1] = -4;

	bool b = 3 > 4 && 3 < 5 || 4 >= 2;
	if (w[2] + 5 > 3) {
		b = 1 != 1;
	}
	else {
		float y = 3.01f;
		y *= 3.2;
	}

	if (!(w[2] == 3)) {
		bool c = !(1 != 2);
		c = b;
		f(3, 4);
	}

	if (a)
		y = (w[1] > 5) ? 10.3 : 5.44;

	;

	return a++;
}

static const int x = 7;
static float y, z = 3.0f, t = -1.32e10F;

int main()
{
	int x = 1;
	while (x++ < 100ul) {
		if (x < 10)
			printf("lt 10");
		if (x == 17)
			break;
		x++;
	}

	for (int i = 0; i < 100; i++) {
		foo(1, 2, 3, 4);
		if (i > 10)
			return 1;
	}

	for (x--; x < 10; x *= 2, ++i)
		printf("%d\n", x);

	for (;;)
		;

	return 0;
}