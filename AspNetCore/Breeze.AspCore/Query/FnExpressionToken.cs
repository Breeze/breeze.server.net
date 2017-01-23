using Breeze.ContextProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breeze.Query {

  // local to this package
  class FnExpressionToken {
    private StringBuilder _sb;
    private int _nextIx;
    private List<FnExpressionToken> _fnArgs;

    private FnExpressionToken() {
      _sb = new StringBuilder();
    }

    public static FnExpression ToExpression(String source, Type entityType) {
      FnExpressionToken token = ParseToken(source, 0);
      return (FnExpression)token.ToExpression(entityType, null);
    }

    private BaseExpression ToExpression(Type entityType, DataType returnDataType) {
      String text = _sb.ToString();
      if (this._fnArgs == null) {

        if (PropertySignature.IsProperty(entityType, text)) {
          // TODO: we could check that the PropExpression dataType is compatible with the returnDataType
          return new PropExpression(text, entityType);
        } else {
          return new LitExpression(text, returnDataType);
        }

      } else {
        String fnName = text;
        var argTypes = FnExpression.GetArgTypes(fnName);
        if (argTypes.Count != _fnArgs.Count) {
          throw new Exception("Incorrect number of arguments to '" + fnName
                  + "' function; was expecting " + argTypes.Count);
        }
        var exprs = _fnArgs.Select((token, ix) => token.ToExpression(entityType, argTypes[ix])).ToList();
        // TODO: we could check that the FnExpression dataType is compatible with the returnDataType
        return new FnExpression(text, exprs);
      }

    }

    private static FnExpressionToken ParseToken(String source, int ix) {
      ix = SkipWhitespace(source, ix);
      var token = CollectQuotedToken(source, ix);
      if (token != null) {
        return token;
      }
      token = new FnExpressionToken();
      String badChars = "'\"";

      while (ix < source.Length) {
        char c = source[ix];

        if (c == '(') {
          ix++;
          ParseFnArgs(token, source, ix);
          return token;
        }

        if (c == ',' || c == ')') {
          token._nextIx = ix;
          return token;
        }

        if (badChars.IndexOf(c) >= 0) {
          throw new Exception("Unable to parse Fn name - encountered: " + c);
        }
        token._sb.Append(c);
        ix++;
      }
      token._nextIx = ix;
      return token;

    }

    private static void ParseFnArgs(FnExpressionToken token, String source, int ix) {
      token._fnArgs = new List<FnExpressionToken>();

      while (ix < source.Length) {
        var argToken = ParseToken(source, ix);
        ix = argToken._nextIx;
        if (argToken._sb.Length != 0) {
          token._fnArgs.Add(argToken);
        }
        char c = source[ix];
        ix++;
        if (c == ')') break;
      }
      token._nextIx = ix;
      return;

    }

    private static int SkipWhitespace(String source, int ix) {
      while (ix < source.Length) {
        char c = source[ix];
        if (c == ' ') {
          ix++;
        } else {
          return ix;
        }
      }
      return ix;
    }

    private static FnExpressionToken CollectQuotedToken(String source, int ix) {
      char c = source[ix];
      if (c != '\'' && c != '"') return null;
      var token = new FnExpressionToken();
      var quoteChar = c;
      ix++;
      while (ix < source.Length) {
        c = source[ix];
        ix++;
        if (c == quoteChar) {
          token._nextIx = ix;
          return token;
        } else {
          token._sb.Append(c);
        }
      }
      throw new Exception("Quoted token was not terminated");
    }
  }

}