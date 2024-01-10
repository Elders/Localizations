using System;

namespace Localizations
{
    public class SafeGet<T>
    {
        readonly T result;

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

        public T Result(T defaultValue = default(T))
        {
            if (Found == true)
                return result;

            return defaultValue;
        }

        public static SafeGet<T> NotFound { get { return new SafeGet<T>(); } }

        public override string ToString()
        {
            return $"Found: '{Found}', Result: '{Result()}'";
        }
    }
}
