using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FactAttribute : Attribute
    {
    }

    public static class Assert
    {
        public static void True(bool condition, string? message = null)
        {
            if (!condition)
            {
                Fail(message ?? "Expected condition to be true.");
            }
        }

        public static void False(bool condition, string? message = null)
        {
            if (condition)
            {
                Fail(message ?? "Expected condition to be false.");
            }
        }

        public static void Equal<T>(T expected, T actual)
        {
            if (!ValuesEqual(expected, actual))
            {
                Fail($"Expected {FormatValue(expected)}, actual {FormatValue(actual)}.");
            }
        }

        public static void Equal(double expected, double actual, int precision)
        {
            var roundedExpected = Math.Round(expected, precision);
            var roundedActual = Math.Round(actual, precision);
            if (!roundedExpected.Equals(roundedActual))
            {
                Fail($"Expected {expected} and actual {actual} to be equal at precision {precision}.");
            }
        }

        public static T IsType<T>(object? value)
        {
            if (value?.GetType() != typeof(T))
            {
                Fail($"Expected type {typeof(T).FullName}, actual {value?.GetType().FullName ?? "<null>"}.");
            }

            return (T)value!;
        }

        public static T IsAssignableFrom<T>(object? value)
        {
            if (value is T typed)
            {
                return typed;
            }

            Fail($"Expected value assignable to {typeof(T).FullName}, actual {value?.GetType().FullName ?? "<null>"}.");
            throw new AssertionException("Unreachable after assertion failure.");
        }

        public static T Single<T>(IEnumerable<T> values)
        {
            var list = values.ToList();
            if (list.Count != 1)
            {
                Fail($"Expected exactly one item, actual {list.Count}.");
            }

            return list[0];
        }

        public static T Single<T>(IEnumerable<T> values, Func<T, bool> predicate)
        {
            return Single(values.Where(predicate));
        }

        public static void Collection<T>(IEnumerable<T> values, params Action<T>[] inspectors)
        {
            var list = values.ToList();
            if (list.Count != inspectors.Length)
            {
                Fail($"Expected collection count {inspectors.Length}, actual {list.Count}.");
            }

            for (var i = 0; i < inspectors.Length; i++)
            {
                inspectors[i](list[i]);
            }
        }

        public static void Contains(string expectedSubstring, string? actualString)
        {
            if (actualString is null || !actualString.Contains(expectedSubstring, StringComparison.Ordinal))
            {
                Fail($"Expected string containing {FormatValue(expectedSubstring)}, actual {FormatValue(actualString)}.");
            }
        }

        public static void Contains<T>(IEnumerable<T> values, Predicate<T> predicate)
        {
            if (!values.Any(value => predicate(value)))
            {
                Fail("Expected collection to contain a matching item.");
            }
        }

        public static void Empty(IEnumerable values)
        {
            foreach (var _ in values)
            {
                Fail("Expected collection to be empty.");
            }
        }

        public static void EndsWith(string expectedEndString, string actualString, StringComparison comparisonType)
        {
            if (!actualString.EndsWith(expectedEndString, comparisonType))
            {
                Fail($"Expected {FormatValue(actualString)} to end with {FormatValue(expectedEndString)}.");
            }
        }

        public static void NotNull([NotNull] object? value)
        {
            if (value is null)
            {
                Fail("Expected value to be non-null.");
            }
        }

        [DoesNotReturn]
        private static void Fail(string message)
        {
            throw new AssertionException(message);
        }

        private static bool ValuesEqual(object? expected, object? actual)
        {
            if (ReferenceEquals(expected, actual))
            {
                return true;
            }

            if (expected is null || actual is null)
            {
                return false;
            }

            if (IsNumeric(expected) && IsNumeric(actual))
            {
                return Convert.ToDecimal(expected, CultureInfo.InvariantCulture) == Convert.ToDecimal(actual, CultureInfo.InvariantCulture);
            }

            if (expected is IEnumerable expectedEnumerable && expected is not string &&
                actual is IEnumerable actualEnumerable && actual is not string)
            {
                return SequencesEqual(expectedEnumerable, actualEnumerable);
            }

            return expected.Equals(actual);
        }

        private static bool SequencesEqual(IEnumerable expected, IEnumerable actual)
        {
            var expectedEnumerator = expected.GetEnumerator();
            var actualEnumerator = actual.GetEnumerator();

            while (true)
            {
                var expectedHasNext = expectedEnumerator.MoveNext();
                var actualHasNext = actualEnumerator.MoveNext();

                if (expectedHasNext != actualHasNext)
                {
                    return false;
                }

                if (!expectedHasNext)
                {
                    return true;
                }

                if (!ValuesEqual(expectedEnumerator.Current, actualEnumerator.Current))
                {
                    return false;
                }
            }
        }

        private static bool IsNumeric(object value)
        {
            return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
        }

        private static string FormatValue(object? value)
        {
            if (value is null)
            {
                return "<null>";
            }

            if (value is string text)
            {
                return $"\"{text}\"";
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                return "[" + string.Join(", ", enumerable.Cast<object?>().Select(FormatValue)) + "]";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? "<unknown>";
        }
    }

    public sealed class AssertionException(string message) : Exception(message);

    public sealed class SkipException(string message) : Exception(message);
}

namespace OSCControl.Compiler.Tests
{
    internal static class Program
    {
        private static async Task<int> Main()
        {
            var tests = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(method => method.GetCustomAttribute<Xunit.FactAttribute>() is not null)
                    .Select(method => (Type: type, Method: method)))
                .OrderBy(test => test.Type.FullName, StringComparer.Ordinal)
                .ThenBy(test => test.Method.Name, StringComparer.Ordinal)
                .ToArray();

            var failed = 0;
            var skipped = 0;

            foreach (var test in tests)
            {
                var displayName = $"{test.Type.Name}.{test.Method.Name}";
                try
                {
                    var instance = Activator.CreateInstance(test.Type);
                    var result = test.Method.Invoke(instance, null);
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                    }

                    Console.WriteLine($"PASS {displayName}");
                }
                catch (Exception exception)
                {
                    var failure = exception is TargetInvocationException && exception.InnerException is not null
                        ? exception.InnerException
                        : exception;

                    if (failure is Xunit.SkipException)
                    {
                        skipped++;
                        Console.WriteLine($"SKIP {displayName}: {failure.Message}");
                        continue;
                    }

                    failed++;
                    Console.Error.WriteLine($"FAIL {displayName}: {failure.GetType().Name}: {failure.Message}");
                }
            }

            Console.WriteLine($"{tests.Length - failed - skipped} passed, {failed} failed, {skipped} skipped, {tests.Length} total");
            return failed == 0 ? 0 : 1;
        }
    }
}
