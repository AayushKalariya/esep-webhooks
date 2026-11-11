using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    public string FunctionHandler(object input, ILambdaContext context)
    {
        // Convert the incoming GitHub JSON into an object.
        dynamic json = JsonConvert.DeserializeObject<dynamic>(
            input.ToString() ?? "{}"
        )!;

        // A GitHub ping does not contain an issue.
        if (json?.issue?.html_url == null)
        {
            return "Ignored: no issue URL";
        }

        string issueUrl = (string)json.issue.html_url;

        // Read the secret URL from Lambda's environment variables.
        string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL")
            ?? throw new InvalidOperationException("SLACK_URL is not set.");

        // Slack expects JSON containing a text property.
        string payload = JsonConvert.SerializeObject(new
        {
            text = $"Issue Created: {issueUrl}"
        });

        using var client = new HttpClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(
                payload,
                Encoding.UTF8,
                "application/json"
            )
        };

        using var response = client.Send(request);
        response.EnsureSuccessStatusCode();

        using var reader = new StreamReader(
            response.Content.ReadAsStream()
        );

        return reader.ReadToEnd();
    }
}
