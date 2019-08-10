using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Memoizer
{
    public class Memoizer
    {
        private ICache cache;

        public Memoizer(ICache cache)
        {
            this.cache = cache;
        }

        public T Memoize<T>(Expression<Func<T>> func)
        {
            var methodCall = func.Body as MethodCallExpression;
            if (methodCall == null)
                throw new ApplicationException("Memoize requires a method call expression to be passed in");

            var obj = methodCall.Object != null ? Expression.Lambda(methodCall.Object).Compile().DynamicInvoke() : null;
            var parameters = methodCall.Arguments.Select(a => Expression.Lambda(a).Compile().DynamicInvoke()).ToArray();
            var key = new List<object> { obj?.GetType().AssemblyQualifiedName ?? methodCall.Method.DeclaringType.AssemblyQualifiedName, methodCall.Method.ToString() };
            key.AddRange(parameters);

            var cresult = cache.Get(key);
            if (cresult != null && cresult is T)
                return (T)cresult;

            var value = (T)Expression.Lambda(Expression.Call(obj != null ? Expression.Constant(obj) : null, methodCall.Method, parameters.Select(p => Expression.Constant(p)))).Compile().DynamicInvoke();
            cache.Store(key, value);
            return value;
        }
    }
}