using System;

namespace Localizations.Contracts
{
    public class SafeGet<T>
    {
        SafeGet()
        {
            Found = false;
        }

        public SafeGet(T result)
        {
            if (ReferenceEquals(null, result) == true) throw new ArgumentNullException(nameof(result));
            Found = true;
            Result = result;
        }

        public bool Found { get; private set; }

        public T Result { get; private set; }

        public static SafeGet<T> NotFound { get { return new SafeGet<T>(); } }
    }
}
