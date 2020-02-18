using System;
using System.Text;
using System.Collections;

namespace SmtuSchedule.Core.Utilities
{
    internal static class ExceptionExtensions
    {
        // Type. \n Message \n Data \n StackTrace \n InnerException
        public static String Format(this Exception exception)
        {
            static String FormatData(IDictionary dictionary)
            {
                StringBuilder keyValuePairs = new StringBuilder(Environment.NewLine);

                foreach (DictionaryEntry entry in dictionary)
                {
                    keyValuePairs.AppendLine($"  {entry.Key} = {entry.Value}");
                }

                return keyValuePairs.ToString();
            }

            StringBuilder report = new StringBuilder();

            report.AppendLine(exception.GetType() + ".");
            report.AppendLine("Message: " + exception.Message);

            if (exception.Data.Count != 0)
            {
                report.Append("Data:" + FormatData(exception.Data));
            }

            report.AppendLine("StackTrace:");
            report.AppendLine(exception.StackTrace);

            if (exception.InnerException != null)
            {
                report.Append("InnerException: " + exception.InnerException.Format());
            }

            return report.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}