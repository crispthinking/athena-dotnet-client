using Resolver.Athena.Grpc;

class Program {
    static readonly Deployment deployment = new()
    {
        DeploymentId = "example-deployment-id",
        Backlog = 100,
    };

    static void Main() {
        Console.WriteLine("Placeholder Athena API Example Client.");
        Console.WriteLine("Printing deployment to ensure packages reference properly");
        Console.WriteLine($"Deployment ID: {deployment.DeploymentId}, Backlog: {deployment.Backlog}");
    }
}
