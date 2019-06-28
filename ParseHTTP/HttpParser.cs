using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ParseHTTP
{
    public class HttpParser
    {
        private readonly string _fileLocation;

        public HttpRequest HttpRequest { get; }

        public HttpResponse HttpResponse { get; }

        public HttpParser(string fileLocation)
        {
            _fileLocation = fileLocation;

            var (rawRequest, rawResponse) = ExtractRawResponseRequestSections();

            var requestKeyValues = GetKeyValuesFrom(rawRequest);

            var responseKeyValues = GetKeyValuesFrom(rawResponse);

            HttpRequest = MapKeysValuesToProperties<HttpRequest>(requestKeyValues);

            HttpResponse = MapKeysValuesToProperties<HttpResponse>(responseKeyValues);
        }

        private (StringBuilder rawRequest, StringBuilder rawResponse) ExtractRawResponseRequestSections()
        {
            var rawRequest = new StringBuilder();
            var rawResponse = new StringBuilder();

            var httpBuilders = new[] { rawRequest, rawResponse };
            int builderIndex = 0;

            using (var fileStream = File.OpenRead(_fileLocation))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    httpBuilders[builderIndex].AppendLine(line);

                    if (line == string.Empty & builderIndex < httpBuilders.Length - 1)
                    {
                        builderIndex++;
                    }
                }
            }

            return (rawRequest, rawResponse);
        }

        private Dictionary<string, string> GetKeyValuesFrom(StringBuilder rawHttp)
        {
            return
                rawHttp.ToString()
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(rr => rr.Contains(':'))
                    .Select(x => x.Split(':'))
                    .ToDictionary(x => SanitizeName(x[0]), x => x[1].Trim());
        }

        private string SanitizeName(string s)
        {
            return s.Trim().Replace("-", string.Empty);
        }

        private T MapKeysValuesToProperties<T>(Dictionary<string, string> keyValues) where T : new()
        {
            var httpElement = new T();

            foreach (var propertyInfo in httpElement.GetType().GetProperties())
            {
                if (!keyValues.ContainsKey(propertyInfo.Name))
                {
                    continue;
                }

                propertyInfo.SetValue(httpElement, keyValues[propertyInfo.Name]);
            }

            return httpElement;
        }
    }
}