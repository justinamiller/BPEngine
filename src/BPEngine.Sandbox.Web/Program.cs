using BPEngine.SDK;

var builder = WebApplication.CreateBuilder(args);

// Build SDK
var cfg = builder.Configuration.GetSection("BPEngine");
var sdkB = BPEngineSdk.Builder().WithPerf();
if (string.Equals(cfg.GetValue("Preset", "cl100k"), "gpt2", StringComparison.OrdinalIgnoreCase))
    sdkB.UsePresetGpt2(cfg["MergesPath"]!, cfg["VocabPath"]);
else
    sdkB.UsePresetCl100k(cfg["RanksPath"]!, cfg["SpecialsPath"]);
if (cfg.GetValue("EnableRag", true) && !string.IsNullOrWhiteSpace(cfg["CorpusPath"]))
    sdkB.EnableRag(cfg["CorpusPath"]!);
builder.Services.AddSingleton(sdkB.Build());

// UI + Swagger
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseStaticFiles();
app.MapRazorPages();

// Minimal APIs
app.MapPost("/api/encode", async (BPEngineSdk s, string text)
    => Results.Ok((await s.EncodeAsync(text ?? string.Empty)).Ids));
app.MapPost("/api/decode", async (BPEngineSdk s, int[] ids)
    => Results.Ok(new { text = (await s.DecodeAsync(ids ?? Array.Empty<int>())).Text }));
app.MapPost("/api/rag/query", (BPEngineSdk s, string q, int k) => Results.Ok(s.QueryRag(q, k)));
app.MapPost("/api/generate/json", async (BPEngineSdk s, string prompt, string schemaJson, bool useRag, int maxNew)
    => Results.Ok(await s.GenerateJsonAsync(prompt, schemaJson, useRag, maxNew)));

app.Run();
