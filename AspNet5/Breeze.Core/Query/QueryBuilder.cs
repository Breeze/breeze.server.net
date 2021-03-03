

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;


namespace Breeze.Core {

  /// <summary>
  /// Used to build up a Queryable.
  /// </summary>
  /// <remarks>
  /// </remarks>
  public class QueryBuilder {

    public static IQueryable ApplyWhere(IQueryable source, Type elementType, BasePredicate predicate) {
      var method = TypeFns.GetMethodByExample((IQueryable<String> q) => q.Where(s => s != null), elementType);
      var lambdaExpr = predicate.ToLambda(elementType);
      var func = BuildIQueryableFunc(elementType, method, lambdaExpr);
      return func(source);
    }



    public static IQueryable ApplySelect(IQueryable source, Type elementType, SelectClause selectClause) {
      var propSigs = selectClause.Properties;
      var dti = DynamicTypeInfo.FindOrCreate(propSigs.Select(ps => ps.Name), propSigs.Select(ps => ps.ReturnType));
      var lambdaExpr = CreateNewLambda(dti, propSigs);
      var method = TypeFns.GetMethodByExample((IQueryable<String> q) => q.Select(s => s.Length), elementType, dti.DynamicType);
      var func = BuildIQueryableFunc(elementType, method, lambdaExpr);
      return func(source);
    }

    public static IQueryable ApplyOrderBy(IQueryable source, Type elementType, OrderByClause orderByClause) {
      var orderByItems = orderByClause.OrderByItems;
      var isThenBy = false;
      orderByItems.ToList().ForEach(obi => {
        var funcOb = QueryBuilder.BuildOrderByFunc(isThenBy, elementType, obi);
        source = funcOb(source);
        isThenBy = true;
      });
      return source;
    }

    public static IQueryable ApplySkip(IQueryable source, Type elementType, int skipCount) {
      var method = TypeFns.GetMethodByExample((IQueryable<String> q) => Queryable.Skip<String>(q, 999), elementType);
      var func = BuildIQueryableFunc(elementType, method, skipCount);
      return func(source);
    }

    public static IQueryable ApplyTake(IQueryable source, Type elementType, int takeCount) {
      var method = TypeFns.GetMethodByExample((IQueryable<String> q) => Queryable.Take<String>(q, 999), elementType);
      var func = BuildIQueryableFunc(elementType, method, takeCount);
      return func(source);
    }

    // TODO: Check if the ThenBy portion of this works
    private static Func<IQueryable, IQueryable> BuildOrderByFunc(bool isThenBy, Type elementType, OrderByClause.OrderByItem obi) {
      var propertyPath = obi.PropertyPath;
      bool isDesc = obi.IsDesc;
      var paramExpr = Expression.Parameter(elementType, "o");
      Expression nextExpr = paramExpr;
      var propertyNames = propertyPath.Split('.').ToList();
      propertyNames.ForEach(pn => {
        var nextElementType = nextExpr.Type;
        var propertyInfo = nextElementType.GetTypeInfo().GetProperty(pn);
        if (propertyInfo == null) {
          throw new Exception("Unable to locate property: " + pn + " on type: " + nextElementType.ToString());
        }
        nextExpr = Expression.MakeMemberAccess(nextExpr, propertyInfo);
      });
      var lambdaExpr = Expression.Lambda(nextExpr, paramExpr);

      var orderByMethod = GetOrderByMethod(isThenBy, isDesc, elementType, nextExpr.Type);

      var baseType = isThenBy ? typeof(IOrderedQueryable<>) : typeof(IQueryable<>);
      var func = BuildIQueryableFunc(elementType, orderByMethod, lambdaExpr, baseType);
      return func;
    }



    private static MethodInfo GetOrderByMethod(bool isThenBy, bool isDesc, Type elementType, Type nextExprType) {
      MethodInfo orderByMethod;
      if (isThenBy) {
        orderByMethod = isDesc
                          ? TypeFns.GetMethodByExample(
                            (IOrderedQueryable<String> q) => q.ThenByDescending(s => s.Length),
                            elementType, nextExprType)
                          : TypeFns.GetMethodByExample(
                            (IOrderedQueryable<String> q) => q.ThenBy(s => s.Length),
                            elementType, nextExprType);
      } else {
        orderByMethod = isDesc
                          ? TypeFns.GetMethodByExample(
                            (IQueryable<String> q) => q.OrderByDescending(s => s.Length),
                            elementType, nextExprType)
                          : TypeFns.GetMethodByExample(
                            (IQueryable<String> q) => q.OrderBy(s => s.Length),
                            elementType, nextExprType);
      }
      return orderByMethod;
    }

    private static LambdaExpression CreateNewLambda(DynamicTypeInfo dti, IEnumerable<PropertySignature> selectors) {
      var paramExpr = Expression.Parameter(selectors.First().InstanceType, "t");
      // cannot create a NewExpression on a dynamic type becasue of EF restrictions
      // so we always create a MemberInitExpression with bindings ( i.e. a new Foo() { a=1, b=2 } instead of new Foo(1,2);
      var newExpr = Expression.New(dti.DynamicEmptyConstructor);
      var propertyExprs = selectors.Select(s => s.BuildMemberExpression(paramExpr));
      var dynamicProperties = dti.DynamicType.GetTypeInfo().GetProperties();
      var bindings = dynamicProperties.Zip(propertyExprs, (prop, expr) => Expression.Bind(prop, expr));
      var memberInitExpr = Expression.MemberInit(newExpr, bindings.Cast<MemberBinding>());
      var newLambda = Expression.Lambda(memberInitExpr, paramExpr);
      return newLambda;
    }

    public static Func<IQueryable, IQueryable> BuildIQueryableFunc(Type instanceType, MethodInfo method) {
      
      var queryableBaseType = typeof(IQueryable<>);
      
      var paramExpr = Expression.Parameter(typeof(IQueryable));
      var queryableType = queryableBaseType.MakeGenericType(instanceType);
      var castParamExpr = Expression.Convert(paramExpr, queryableType);

      var callExpr = Expression.Call(method, castParamExpr );
      var castResultExpr = Expression.Convert(callExpr, typeof(IQueryable));
      var lambda = Expression.Lambda(castResultExpr, paramExpr);
      var func = (Func<IQueryable, IQueryable>)lambda.Compile();
      return func;
    }

    public static Func<IQueryable, IQueryable> BuildIQueryableFunc<TArg>(Type instanceType, MethodInfo method, TArg parameter, Type queryableBaseType = null) {
      if (queryableBaseType == null) {
        queryableBaseType = typeof(IQueryable<>);
      }
      var paramExpr = Expression.Parameter(typeof(IQueryable));
      var queryableType = queryableBaseType.MakeGenericType(instanceType);
      var castParamExpr = Expression.Convert(paramExpr, queryableType);


      var callExpr = Expression.Call(method, castParamExpr, Expression.Constant(parameter));
      var castResultExpr = Expression.Convert(callExpr, typeof(IQueryable));
      var lambda = Expression.Lambda(castResultExpr, paramExpr);
      var func = (Func<IQueryable, IQueryable>)lambda.Compile();
      return func;
    }
  }
}
