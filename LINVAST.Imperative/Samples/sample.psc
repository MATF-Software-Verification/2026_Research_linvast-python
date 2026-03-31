// comment

algorithm test
begin
	declare real pi = 3.14
	declare integer x = 4
	declare real x = (1 + x) * (1 - pi / 4)
	declare integer array arr
	declare real list lst
	declare point set points
	declare boolean x = False
	
	/* prints multiple arguments */
	procedure print(arr : string array)
	begin
		declare integer i
		declare integer n = call sizeof(arr)
		i = 0
		while i < n do begin
			call print(arr[i])
			increment i
		end
	end

	function fib(n : integer) returning integer
	begin
		if n < 0 then error "requiring positive integer"
		else if n <= 1 then return n
		else return call fib(n-1) + call fib(n-2)
	end

	repeat begin
		declare integer k = 3
		k = k + 1
	end until k > 4
end
