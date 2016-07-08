using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Breeze.ContextProvider;
using System.Reflection;

namespace OData.Linq
{
	internal class EFCoreCompatibleVisitor : ExpressionVisitor
	{
		protected override Expression VisitBinary(BinaryExpression node)
		{
			Expression left = Visit(node.Left);
			Expression right = Visit(node.Right);
			//if (node.Left.NodeType == ExpressionType.MemberAccess)
			//{
			//	var memberExpression = node.Left as MemberExpression;
			//	var expression = Visit(memberExpression.Expression);
			//}
			//if (node.Right.NodeType == ExpressionType.MemberAccess)
			//{
			//	var memberExpression = node.Right as MemberExpression;
			//	var expression = Visit(memberExpression.Expression);
			//}

			// get prevailing type
			var leftType = left.Type;
			var rightType = right.Type;
			var prevailingType = leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(Nullable<>) ? leftType :
				rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(Nullable<>) ? rightType : null;
			
			if (prevailingType != null)
			{
                if (leftType != prevailingType)
                {
                    if (leftType == typeof(bool) && right.NodeType == ExpressionType.Constant)
                    {
                        var rightConst = right as ConstantExpression;
                        var rightVal = (bool)rightConst.Value;
                        if (rightVal)
                            return left;
                        else
                            return Expression.Negate(left);
                    }
                    left = Expression.Convert(left, prevailingType);
                }
				if (rightType != prevailingType)
                    if (rightType == typeof(bool) && left.NodeType == ExpressionType.Constant)
                    {
                        var leftConst = left as ConstantExpression;
                        var leftVal = (bool)leftConst.Value;
                        if (leftVal)
                            return right;
                        else
                            return Expression.Negate(right);
                    }

                right = Expression.Convert(right, prevailingType);
			}
			try
			{
                return Expression.MakeBinary(node.NodeType, left, right);
                //return node;
			}
			catch (Exception ex)
			{
				throw ex;
			}

			//// look for MemberAccess
			//if (node.Left.NodeType == ExpressionType.MemberAccess || node.Right.NodeType == ExpressionType.MemberAccess)
			//{

			//	// eliminate null cases where SQL would always ignore
			//	if (left == null)
			//	{
			//		return right;
			//	}

			//	if (right == null)
			//	{
			//		return left;
			//	}

			//	if (node.NodeType == ExpressionType.NotEqual &&
			//		(IsNullConstant(right) || IsNullConstant(left)))
			//	{
			//		return null;
			//	}

				

			//	////if (left.NodeType == ExpressionType.MemberAccess)
			//	//{
			//	//	var memberType = left.Type;
			//	//	if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>))
			//	//	{
			//	//		//var uType = Nullable.GetUnderlyingType(memberType);
			//	//		//left = Expression.Convert(left, uType);
			//	//	}
			//	//	// if not nullable type, convert it to one
			//	//	else if (prevailingType != null)
			//	//	{
			//	//		//left = Expression.Convert(left, prevailingType);
			//	//	}
			//	//}

			//	////if (right.NodeType == ExpressionType.MemberAccess)
			//	//{
			//	//	var memberType = right.Type;
			//	//	if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>))
			//	//	{
			//	//		//var uType = Nullable.GetUnderlyingType(memberType);
			//	//		//right = Expression.Convert(right, uType);
			//	//	}
			//	//	else if (prevailingType != null)
			//	//	{
			//	//		//right = Expression.Convert(right, prevailingType);
			//	//	}
			//	//}

			//	try
			//	{
			//		return Expression.MakeBinary(node.NodeType, left, right);
			//	}
			//	catch (Exception ex)
			//	{
			//		throw ex;
			//	}
			//}

			//try
			//{
			//	return base.VisitBinary(node);
			//}
			//catch (Exception ex)
			//{
			//	throw ex;
			//}
		}

        private bool _yankingNull;
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (_yankingNull &&
                node.NodeType == ExpressionType.Convert &&
                Nullable.GetUnderlyingType(node.Type) == typeof(bool))
            {
                return Visit(node.Operand);
            }

            return base.VisitUnary(node);
        }
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            Expression expression;
            if (TryRemoveNullPropagation(node, out expression))
            {
                return expression;
            }

            if (_yankingNull && IsNullCheck(node.Test))
            {
                return Visit(node.IfFalse);
            }

            return base.VisitConditional(node);
        }

        private bool TryRemoveNullPropagation(ConditionalExpression node, out Expression condition)
        {
            condition = null;
            if (node.IfTrue.NodeType != ExpressionType.Constant)
            {
                return false;
            }

            if (node.Test.NodeType != ExpressionType.Equal)
            {
                return false;
            }

            var test = (BinaryExpression)node.Test;
            var constantExpr = (ConstantExpression)node.IfTrue;

            if (node.IfFalse.NodeType != ExpressionType.Call)
            {
                return false;
            }

            var memberExpr = (MethodCallExpression)node.IfFalse;

            _yankingNull = true;
            condition = Visit(memberExpr);
            _yankingNull = false;
            return true;
        }


        protected override Expression VisitMember(MemberExpression memberExpression)
		{
			// Recurse down to see if we can simplify...
			var expression = Visit(memberExpression.Expression);

			// If we've ended up with a constant, and it's a property or a field,
			// we can simplify ourselves to a constant
			if (expression is ConstantExpression)
			{
				object container = ((ConstantExpression)expression).Value;
				var member = memberExpression.Member;
				if (member is FieldInfo)
				{
					object value = ((FieldInfo)member).GetValue(container);
					return Expression.Constant(value);
				}
				if (member is PropertyInfo)
				{
					object value = ((PropertyInfo)member).GetValue(container, null);
					return Expression.Constant(value);
				}
			}
			return base.VisitMember(memberExpression);
		}

		private bool IsNullCheck(Expression expression)
		{
			if (expression.NodeType != ExpressionType.Equal)
			{
				return false;
			}

			var binaryExpr = (BinaryExpression)expression;
			return IsNullConstant(binaryExpr.Right);
		}

		private bool IsNullConstant(Expression expression)
		{
			return expression.NodeType == ExpressionType.Constant &&
				   ((ConstantExpression)expression).Value == null;
		}


		public interface IHasValue<T>
		{
			Object Value { get; }
		}

		//private static Expression GetValueExpression(Expression<Func<int?>> expr)
		//{
		//	Expression<Func<int?, int>> result = it => expr.ody.Value;
		//	return result;
		//}
	}

    internal class OrderByRemover : ExpressionVisitor
    {
        public bool modified = false;
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType != typeof(Enumerable) && node.Method.DeclaringType != typeof(Queryable))
                return base.VisitMethodCall(node);

            if (node.Method.Name != "OrderBy" && node.Method.Name != "OrderByDescending" && node.Method.Name != "ThenBy" && node.Method.Name != "ThenByDescending")
                return base.VisitMethodCall(node);

            //eliminate the method call from the expression tree by returning the object of the call.
            modified = true;
            return base.Visit(node.Arguments[0]);
        }
    }
}