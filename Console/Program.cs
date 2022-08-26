// See https://aka.ms/new-console-template for more information
using LiveChatLib2;

Console.WriteLine("Hello, World!");
try
{
    var service = new LiveChatService();

    await service.Deploy(default);
}
catch (Exception ex)
{
    Console.WriteLine($"Application failed: {ex.Message}\n{ex.StackTrace}");
}