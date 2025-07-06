using WebSocketClientWithLibrary.Clients;


Console.Write("🔑 Введи токен: ");
var token = Console.ReadLine()?.Trim();


if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("❌ Токен не може бути порожнім.");
    return;
}

token = token.Replace("\"", "").Trim();

Console.WriteLine($"🌐 Формуємо URI: wss://platform.fintacharts.com/api/streaming/ws/v1/realtime?token={token}");

Console.Write("🎯 Введи instrumentId: ");
var instrumentId = Console.ReadLine();

var client = new FintaWebSocketClient(token!);

var tcs = new TaskCompletionSource<decimal>();

client.OnMessageReceived += (msg) =>
{
    if (msg.Type == "l1-update" && msg.Last != null)
    {
        Console.WriteLine($"📈 Отримано ціну: {msg.Last.Price}");
        tcs.TrySetResult(msg.Last.Price);
    }
};

await client.ConnectAsync();
await client.SubscribeAsync(instrumentId!);

// Чекаємо або на ціну, або на таймаут
var timeout = Task.Delay(5000);
var completed = await Task.WhenAny(tcs.Task, timeout);

await client.UnsubscribeAsync(instrumentId!);

if (completed == timeout)
    Console.WriteLine("⚠️ Не вдалося отримати ціну.");
else
    Console.WriteLine($"✅ Актуальна ціна: {tcs.Task.Result}");