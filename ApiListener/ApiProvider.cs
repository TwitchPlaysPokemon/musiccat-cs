using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApiListener
{
    // TODO: Rewrite as an attribute?
    /// <summary>
    /// Inherit from this class to create an API endpoint provider
    /// </summary>
    public abstract class ApiProvider
    {
        public abstract IEnumerable<ApiCommand> Commands { get; }

        private class ApiMissingParameterError : ApiError
        {
            public ApiMissingParameterError(string message = null) : base(message) { }
        }

        protected T ParseRequired<T>(IEnumerable<string> args, int index, Func<string, T> process, string name, string invalidError = null)
        {
            if (args.Count() <= index)
            {
                throw new ApiMissingParameterError($"Parameter {name} is missing");
            }
            try
            {
                return process(args.ElementAt(index));
            }
            catch (ApiError)
            {
                throw;
            }
            catch
            {
                throw new ApiError(invalidError ?? $"Provided {name} \"{args.ElementAt(index)}\" is invalid");
            }
        }

        protected T? ParseOptional<T>(IEnumerable<string> args, int index, Func<string, T> process, string name, string invalidError = null) where T : struct
        {
            try
            {
                return ParseRequired(args, index, process, name, invalidError);
            }
            catch (ApiMissingParameterError)
            {
                return null;
            }
        }
    }

}
