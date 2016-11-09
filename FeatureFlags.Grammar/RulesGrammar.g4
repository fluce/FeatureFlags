grammar RulesGrammar;

@header {
using System.Linq.Expressions;
using System.Linq;
using System.Globalization;
using System;
}

tokens { AND, OR, NOT, DOT, COMMA, LT, GT, LTE, GTE, EQUAL, NEQUAL, DOT, COMMA, PLUS, MINUS, MULT, DIV, MOD }

  AND : ('&' | 'and');
  OR  : ('|' | 'or');
  NOT : ('~' | 'not' | '!');
  LT : '<';
  LTE : '<=';
  GT : '>';
  GTE : '>=';
  EQUAL : ('='|'==');
  NEQUAL : ('!='|'<>');
  DOT : '.';
  COMMA : ',';
  PLUS : '+';
  MINUS : '-';
  MULT : '*';
  DIV : '/';
  MOD : '%';
  TRUE : 'true';
  FALSE : 'false';

  fragment NUMBER : '0'..'9';
  fragment LETTER : ('a'..'z' | 'A'..'Z' | '_');
  ID : LETTER(LETTER|NUMBER)* ;

  INTEGER : NUMBER+;
  FLOAT : NUMBER*'.'NUMBER+;
  PERCENT : NUMBER*(|'.'NUMBER+)'%';
  BOOLEAN : (TRUE|FALSE);

  DATE1 : NUMBER NUMBER NUMBER NUMBER '/' NUMBER NUMBER '/' NUMBER NUMBER;
  DATE2 : NUMBER NUMBER NUMBER NUMBER '-' NUMBER NUMBER '-' NUMBER NUMBER;
  HOUR : NUMBER NUMBER ':' NUMBER NUMBER;

  STRING : '"' (~[\r\n"] | '""')* '"' 
	{
		string s = Text;
		s = s.Substring(1, s.Length - 2); // strip the leading and trailing quotes
		s = s.Replace("\"\"", "\""); // replace all double quotes with single quotes
		Text = s;
	}	
	;
  
  

/*
 * Parser Rules
 */

compileUnit 
	:	expression EOF
	;

expression returns [Expression ret]
	:	'(' inner=expression ')' { $ret=$inner.ret; }  # Parenthesis
	|	op=NOT param=expression { try { $ret=Expression.Not($param.ret); } catch (Exception e) { throw handle(Context, e); } } # UnaryOperator
	|	op=PLUS param=expression  { try { $ret=Expression.UnaryPlus(c($param.ret)); } catch (Exception e) { throw handle(Context, e); } } # ArithmUnaryOperator
	|	op=MINUS param=expression  { try { $ret=Expression.Negate(c($param.ret)); } catch (Exception e) { throw handle(Context, e); } } # ArithmUnaryOperator
	|	left=expression op=MULT right=expression { $ret=bin(Context, ExpressionType.Multiply, $left.ret,$right.ret); }  # ArithmBinaryOperator
	|	left=expression op=DIV right=expression { $ret=bin(Context, ExpressionType.Divide, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=MOD right=expression { $ret=bin(Context, ExpressionType.Modulo, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=PLUS right=expression { $ret=bin(Context, ExpressionType.Add, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=MINUS right=expression { $ret=bin(Context, ExpressionType.Subtract, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=LT middle=expression op=LT right=expression { $ret=ternary(Context, $left.ret, ExpressionType.LessThan, $middle.ret, ExpressionType.LessThan, $right.ret); } # BinaryOperator
	|	left=expression op=LTE middle=expression op=LT right=expression { $ret=ternary(Context, $left.ret, ExpressionType.LessThanOrEqual, $middle.ret, ExpressionType.LessThan, $right.ret); } # BinaryOperator
	|	left=expression op=LT right=expression { $ret=bin(Context, ExpressionType.LessThan, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=LTE right=expression { $ret=bin(Context, ExpressionType.LessThanOrEqual, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=GT right=expression { $ret=bin(Context, ExpressionType.GreaterThan, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=GTE right=expression { $ret=bin(Context, ExpressionType.GreaterThanOrEqual, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=EQUAL right=expression { $ret=bin(Context, ExpressionType.Equal, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=NEQUAL right=expression { $ret=bin(Context, ExpressionType.NotEqual, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=AND right=expression { $ret=bin(Context, ExpressionType.And, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	left=expression op=OR right=expression { $ret=bin(Context, ExpressionType.Or, $left.ret,$right.ret); } # ArithmBinaryOperator
	|	TRUE { try { $ret=Expression.Constant(true); } catch (Exception e) { throw handle(Context, e); } } # Constant
	|	FALSE { try { $ret=Expression.Constant(false); } catch (Exception e) { throw handle(Context, e); } } # Constant
	|	symbol=ID '(' prm+=expression (COMMA prm+=expression)* ')' 
							{ 
								try {
									var m=GetGlobalMethod($symbol.text);
									if (m==null) throw new SemanticException(Context, $"Error at {Context.Start.Line}:{Context.Start.Column} : unknown global method {Context.Start.Text}" );
									$ret=Expression.Call(GetGlobal(),m,$prm.Select(x=>x.ret)); 
								} catch (Exception e) { throw handle(Context, e); }
							}  # GlobalMethodCall
	|	o=obj DOT symbol=ID '(' prm+=expression (COMMA prm+=expression)* ')' 
							{ 
								try {
									var m=GetMethod($o.ret.Type,$symbol.text,$prm.Select(x=>x.ret.Type).ToArray());
									if (m==null) throw new SemanticException(Context, $"Error at {Context.Start.Line}:{Context.Start.Column} : unknown method {Context.Start.Text}");
									$ret=Expression.Call($o.ret,m,$prm.Select(x=>x.ret)); 
								} catch (Exception e) { throw handle(Context, e); }
							}  # MethodCall
	|	o=obj { $ret=$o.ret; } # Variable
	;

date returns [DateTime datetime]
	: '\'' d=DATE1 '\'' { try { $datetime=DateTime.ParseExact($d.text, "yyyy/MM/dd", CultureInfo.InvariantCulture); } catch (Exception e) { throw handle(Context, e); } }
	| '\'' d=DATE2 '\'' { try { $datetime=DateTime.ParseExact($d.text, "yyyy-MM-dd", CultureInfo.InvariantCulture); } catch (Exception e) { throw handle(Context, e); } }
	| '\'' d=DATE1 h=HOUR '\'' { try { $datetime=DateTime.ParseExact($d.text+" "+$h.text, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture); } catch (Exception e) { throw handle(Context, e); } }
	| '\'' d=DATE2 h=HOUR '\'' { try { $datetime=DateTime.ParseExact($d.text+" "+$h.text, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture); } catch (Exception e) { throw handle(Context, e); } }
	;

time returns [TimeSpan timespan]
	: '\'' h=HOUR '\'' { try { $timespan=TimeSpan.Parse($h.text, CultureInfo.InvariantCulture); } catch (Exception e) { throw handle(Context, e); } }
	;

obj returns [Expression ret, string name]
	:	date_cst=date { try { $ret=Expression.Constant($date_cst.datetime);} catch (Exception e) { throw handle(Context, e); } }
	|	time_cst=time { try { $ret=Expression.Constant($time_cst.timespan);} catch (Exception e) { throw handle(Context, e); } }
	|	constant=(INTEGER|FLOAT) { try { $ret=Expression.Constant(decimal.Parse($constant.text,CultureInfo.InvariantCulture)); } catch (Exception e) { throw handle(Context, e); } }
	|	constant=PERCENT { try { $ret=Expression.Constant(decimal.Parse($constant.text.TrimEnd('%'),CultureInfo.InvariantCulture)/100m); } catch (Exception e) { throw handle(Context, e); } }
	|	constant=STRING { try { $ret=Expression.Constant($constant.text); } catch (Exception e) { throw handle(Context, e); } }
	|	symbol=ID { try { $ret=Expression.Property(GetGlobal(),$symbol.text); $name=$symbol.text; } catch (Exception e) { throw handle(Context, e); } }
	|	o=obj DOT symbol=ID { try { $ret=Expression.Property($o.ret,$symbol.text); $name=$o.name+"."+$symbol.text;  } catch (Exception e) { throw handle(Context, e); } }
	;
/*
 * Lexer Rules
 */

WS
	:	' ' -> channel(HIDDEN)
	;
