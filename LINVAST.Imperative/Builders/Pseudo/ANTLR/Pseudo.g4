/* 
	PoC pseudo-code like grammar
 */

grammar Pseudo;

unit
	: 'algorithm' NAME block EOF
	;

block
	: 'begin' statement+ 'end'
	| statement
	;

statement
	: 'pass'
	| declaration
	| assignment
	| cexp
	| 'return' exp
	| 'error' STRING
	| 'if' exp 'then' block ('else' block)? 
	| 'while' exp 'do' block 
	| 'repeat' block 'until' exp
	| ('increment' | 'decrement') var	
	;

declaration
	: 'declare' type NAME ('=' exp)? 
	| 'procedure' NAME '(' parlist? ')' block 
	| 'function' NAME '(' parlist? ')' 'returning' type block 
	;

parlist
	: NAME ':' type (',' NAME ':' type)*
	;

assignment
	: var '=' exp
	;

exp
	: literal 
	| var
	| '(' exp ')'
	| exp aop exp
	| exp rop exp
	| exp lop exp
	| uop exp
	| cexp
	;

aexp
	: exp aop exp
	;

lexp
	: 'True'
	| 'False'
	| exp lop exp
	;

aop : '+' | '-' | '*' | '/' | 'div' | 'mod' ;
rop : '>' | '>=' | '<' | '<=' | '==' | '=/=' ;
lop : 'and' | 'or' ;
uop: '-' | 'not' ;

cexp
	: 'call' NAME '(' explist? ')'
	;

explist
	: exp (',' exp)*
	;

literal : 'True' | 'False' | INT | HEX | FLOAT | HEX_FLOAT | STRING ;

type 
	: typename 'array'?
	| typename 'list'?
	| typename 'set'?
	;

typename 
	: 'integer' 
	| 'real' 
	| 'string' 
	| NAME 
	;

var 
	: NAME ('[' iexp ']')?
	;

iexp 
	: literal
	| var
	| aexp
	;

// Lexer

NAME
    : [a-zA-Z_][a-zA-Z_0-9]*
    ;

STRING
    : '"' ( EscapeSequence | ~('\\'|'"') )* '"' 
    ;

fragment
EscapeSequence
    : '\\' [abfnrtvz"'\\]
    | '\\' '\r'? '\n'
    | HexEscape
    ;

fragment
HexEscape
    : '\\' 'x' HexDigit HexDigit
    ;

INT
    : Digit+
    ;

HEX
    : '0' [xX] HexDigit+
    ;

FLOAT
    : Digit+ '.' Digit* ExponentPart?
    | '.' Digit+ ExponentPart?
    | Digit+ ExponentPart
    ;

HEX_FLOAT
    : '0' [xX] HexDigit+ '.' HexDigit* HexExponentPart?
    | '0' [xX] '.' HexDigit+ HexExponentPart?
    | '0' [xX] HexDigit+ HexExponentPart
    ;

fragment
ExponentPart
    : [eE] [+-]? Digit+
    ;

fragment
HexExponentPart
    : [pP] [+-]? Digit+
    ;

fragment
Digit
    : [0-9]
    ;

fragment
HexDigit
    : [0-9a-fA-F]
    ;

BlockComment
    :   '/*' .*? '*/'  -> skip
    ;

LineComment
    :   '//' ~[\r\n]*  -> skip
    ;

WS  
    : [ \t\u000C\r\n]+ -> skip
    ;
