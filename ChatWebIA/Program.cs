using LLama.Common;
using LLama;

// --- ATENÇÃO, ALUNO! ---
// Este é o caminho para o nosso modelo.
// Certifique-se de que a pasta 'modelos' está dentro da pasta principal do seu projeto.
var projectDirectory = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName 
    ?? throw new DirectoryNotFoundException("Could not find project directory");
var modelPath = Path.Combine(projectDirectory, "modelos", "gemma3-1b-Q8_0.gguf");

// Parâmetros para carregar o modelo. GpuLayerCount = 0 significa que usaremos a CPU.
var parameters = new ModelParams(modelPath) {
    GpuLayerCount = 0
};

// Carrega o modelo na memória. Faremos isso uma única vez quando o servidor iniciar.
// A linha 'using var' garante que os recursos sejam liberados corretamente.
using var model = LLamaWeights.LoadFromFile(parameters);

// O 'Singleton' garante que a mesma instância do modelo seja usada para todos os usuários do chat.
var executor = new InteractiveExecutor(model.CreateContext(parameters));

// --- Configuração do Servidor Web ---
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Estas duas linhas dizem ao nosso servidor para procurar e mostrar arquivos
// como o nosso 'index.html' que vamos criar.
app.UseDefaultFiles();
app.UseStaticFiles();

// --- A "Ponte" (API) que conversa com o nosso site ---
// Quando o site enviar uma mensagem para o endereço '/chat', este código será executado.
app.MapPost("/chat", async (ChatRequest request) => {
    Console.WriteLine($"Recebida a mensagem: {request.Message}");

    // Prepara o prompt para a IA no formato do Gemma 3
    var prompt =
    $"<start_of_turn>user\n{request.Message}<end_of_turn>\n<start_of_turn>model\n";
    string responseText = "";

    // Pede para a IA gerar uma resposta, token por token
    await foreach (var text in executor.InferAsync(prompt, new InferenceParams() {
        AntiPrompts = ["<end_of_turn>"]
    })) {
        responseText += text;
    }

    Console.WriteLine($"Resposta da IA: {responseText}");

    // Retorna a resposta completa para o site
    return Results.Ok(new { Response = responseText });
});

// Inicia o servidor!
app.Run();

// Classe pequena para representar a mensagem que vem do nosso site.
public record ChatRequest(string Message);
