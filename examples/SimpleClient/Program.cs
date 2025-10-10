using Resolver.Athena.Grpc;

class Program
{
    static readonly Deployment s_deployment = new()
    {
        DeploymentId = "example-deployment-id",
        Backlog = 100,
    };

    static void Main()
    {
        Console.WriteLine("Placeholder Athena API Example Client.");
        Console.WriteLine("Printing deployment to ensure packages reference properly");
        Console.WriteLine($"Deployment ID: {s_deployment.DeploymentId}, Backlog: {s_deployment.Backlog}");
    }
}
