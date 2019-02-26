using System;
using TracerAttributes;

namespace X42.Utilities
{
    /// <summary>
    ///     Collection of guard methods.
    ///     <para>
    ///         Guards are typically used at the beginning of a method to protect the body of
    ///         the method being called with invalid set of parameters or object states.
    ///     </para>
    /// </summary>
    public static class Guard
    {

        /// <summary>
        /// Throws an ArgumentNullException if input is null.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Null<T>(T input, string parameterName)
        {
            if (input == null) { throw new ArgumentNullException(parameterName); }

        }

        /// <summary>
        /// Throws an ArgumentNullException if input is null.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Null<T>(T input, string parameterName, string errorMsg)
        {
            // We don't have to error we can use the call without an error msg
            if (string.IsNullOrWhiteSpace(errorMsg))
            {
                Guard.Null(input, parameterName);
                return;
            }

            if (input == null) { throw new ArgumentNullException(parameterName, errorMsg); }

        }

        /// <summary>
        ///     Checks an object is not null.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The object.</param>
        /// <param name="parameterName">The name of the object.</param>
        /// <returns>The object if it is not null.</returns>
        /// <exception cref="ArgumentNullException">An exception if the object passed is null.</exception>
        [NoTrace]
        public static T NotNull<T>(T value, string parameterName)
        {
            // the parameterName should never be null or empty
            if (string.IsNullOrWhiteSpace(parameterName)) throw new ArgumentNullException(parameterName);

            // throw if the value is null
            if (ReferenceEquals(value, null)) throw new ArgumentNullException(parameterName);

            return value;
        }

        /// <summary>
        ///     Checks a <see cref="string" /> is not null or empty.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="parameterName">The name of the string.</param>
        /// <returns>The string if it is not null or empty.</returns>
        [NoTrace]
        public static string NotEmpty(string value, string parameterName)
        {
            NotNull(value, parameterName);

            if (value.Trim().Length == 0)
                throw new ArgumentException($"The string parameter {parameterName} cannot be empty.");

            return value;
        }

        /// <summary>
        /// Throws an Exception if the condition is not true
        /// </summary>   
        /// <exception cref="Exception"></exception>     
        public static void AssertTrue(bool condition)
        {
            if (!condition) { throw new Exception("Assertion Failed! Expected 'true'"); }
        }


        /// <summary>
        /// Throws an Exception if the condition is not true
        /// </summary>   
        /// <exception cref="Exception"></exception>        
        public static void AssertTrue(bool condition, string errorMsg)
        {
            // We don't have to error we can use the call without an error msg
            if (string.IsNullOrWhiteSpace(errorMsg))
            {
                Guard.AssertTrue(condition);
                return;
            }

            if (!condition) { throw new Exception(errorMsg); }
        }

        /// <summary>
        /// Throws an Exception if the condition is not false
        /// </summary>        
        public static void AssertFalse(bool condition)
        {
            if (!condition) { throw new Exception("Assertion Failed! Expected 'false'"); }
        }
        
        /// <summary>
        /// Throws an Exception if the condition is not false
        /// </summary>
        /// <exception cref="Exception"></exception>           
        public static void AssertFalse(bool condition, string errorMsg)
        {
            // We don't have to error we can use the call without an error msg
            if (string.IsNullOrWhiteSpace(errorMsg))
            {
                Guard.AssertFalse(condition);
                return;
            }

            if (!condition) { throw new Exception(errorMsg); }
        }


        /// <summary>
        /// Throws an ArgumentNullException if input is null.
        /// Throws an ArgumentException if input is an empty string.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void NullOrEmpty(string input, string parameterName)
        {
            Guard.Null(input, parameterName);

            if (input == string.Empty) { throw new ArgumentException($"Required input {parameterName} was empty.", parameterName); }
        }


        /// <summary>
        /// Throws an ArgumentNullException if input is null.
        /// Throws an ArgumentException if input is an empty string.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void NullOrEmpty(string input, string parameterName, string errorMsg)
        {
            Guard.Null(input, parameterName);

            // We don't have to error we can use the call without an error msg
            if (string.IsNullOrWhiteSpace(errorMsg))
            {
                Guard.NullOrEmpty(input, parameterName);
                return;
            }

            if (input == string.Empty) { throw new ArgumentException(errorMsg, parameterName); }
        }
    }
}