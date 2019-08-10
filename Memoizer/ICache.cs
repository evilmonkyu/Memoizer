using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memoizer
{
    public interface ICache
    {
        void Store(object key, object value);

        object Get(object key);
    }
}