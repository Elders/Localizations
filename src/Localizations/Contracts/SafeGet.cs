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
            this.result = result;
        }

        public bool Found { get; private set; }

        T result { get; set; }

        public T Result(T defaultValue = default(T))
        {
            if (Found == true)
                return result;

            return defaultValue;
        }

        public static SafeGet<T> NotFound { get { return new SafeGet<T>(); } }
    }
}
