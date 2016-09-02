using System;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public static class TestUtilities
    {
        public static void AssertAggregateException(Action action, string message)
        {
            AssertUnwrappedMessage<AggregateException>(action, message);
        }

        public static void AssertAggregateException<T>(Action action, string message)
        {
            AssertUnwrappedException<AggregateException>(action, message, typeof(T));
        }

        public static void AssertUnwrappedMessage<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                Assert.Equal(Unwrap(ex)?.Message, message);
            }
        }

        public static void AssertUnwrappedException<T>(Action action, string message, Type expectedExceptionType) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                Exception unwrappedException = Unwrap(ex);

                Assert.IsType(expectedExceptionType, unwrappedException);
                Assert.Equal(message, unwrappedException.Message);
            }
        }

        public static void AssertUnwrappedException<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Exception unwrappedException = Unwrap(ex);

                Assert.IsType(typeof(T), unwrappedException);
            }
        }

        private static Exception Unwrap(Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            var next = ex.GetBaseException();
            while (next.InnerException != null)
            {
                // On mono GetBaseException() doesn't seem to do anything
                // so just walk the inner exception chain.
                next = next.InnerException;
            }

            return next;
        }
    }
}
