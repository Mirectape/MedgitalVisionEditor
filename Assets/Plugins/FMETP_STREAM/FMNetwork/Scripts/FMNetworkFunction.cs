using System;

namespace FMSolution.FMNetwork
{
    [Serializable]
    public class FMNetworkFunction
    {
        public string FunctionName = "";
        public string[] Variables = new string[0];
        public FMNetworkFunction(string inputFunctionName, object[] inputVariables = null)
        {
            FunctionName = inputFunctionName;
            if (inputVariables != null) Variables = Array.ConvertAll(inputVariables, p => (p ?? String.Empty).ToString());
        }
    }
}