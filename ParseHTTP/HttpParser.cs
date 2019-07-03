using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ParseHTTP
{
    public class HttpParser
    {
        private const int AsciiNewLine = 10;
        private const int AsciiCarriageReturn = 13;

        private readonly string _fileLocation;

        private Queue<int> _previousFourBytes;

        public HttpRequest HttpRequest { get; }

        public HttpResponse HttpResponse { get; }

        public HttpParser(string fileLocation)
        {
            PrimePreviousFourByteQueue();

            _fileLocation = fileLocation;

            var (requestBytes, responseHeaderBytes, responseBodyBytes) = ExtractRawResponseRequestSections();

            var requestKeyValues = GetKeyValuesFrom(rawHttp: Encoding.ASCII.GetString(requestBytes.ToArray()));

            var responseHeaderKeyValues = GetKeyValuesFrom(Encoding.ASCII.GetString(responseHeaderBytes.ToArray()));

            HttpRequest = MapKeysValuesToProperties<HttpRequest>(requestKeyValues);

            HttpResponse = MapKeysValuesToProperties<HttpResponse>(responseHeaderKeyValues);

            HttpResponse.Body = responseBodyBytes;
        }

        private void PrimePreviousFourByteQueue()
        {
            _previousFourBytes = new Queue<int>();

            for (int i = 0; i < 4; i++)
            {
                _previousFourBytes.Enqueue(-1);
            }
        }

        private (IEnumerable<byte> rawRequest, IEnumerable<byte> rawResponse, IEnumerable<byte> responseBody) ExtractRawResponseRequestSections()
        {
            var rawRequest = new List<byte>();
            var rawResponseHeaders = new List<byte>();
            var rawResponseBody = new List<byte>();

            var httpBuilder = new[] { rawRequest, rawResponseHeaders, rawResponseBody };

            int currentBuilderIndex = 0;

            var separator = new[] { AsciiCarriageReturn, AsciiNewLine, AsciiCarriageReturn, AsciiNewLine };

            using (var fileStream = File.OpenRead(_fileLocation))
            {
                int currentByteValue;
                while ((currentByteValue = fileStream.ReadByte()) != -1)
                {
                    httpBuilder[currentBuilderIndex].Add(Convert.ToByte(currentByteValue));

                    var previousFourBytes = GetPreviousFourBytes(currentByteValue);

                    if (previousFourBytes.SequenceEqual(separator))
                    {
                        currentBuilderIndex++;
                    }
                }
            }

            return (rawRequest, rawResponseHeaders, rawResponseBody);
        }

        private IEnumerable<int> GetPreviousFourBytes(int currentByteValue)
        {
            _previousFourBytes.Dequeue();
            _previousFourBytes.Enqueue(currentByteValue);

            return _previousFourBytes.ToArray();
        }

        private Dictionary<string, string> GetKeyValuesFrom(string rawHttp)
        {
            return
                rawHttp
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