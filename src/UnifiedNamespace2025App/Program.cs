using HmiLib;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Text.Json;
using UnifiedNamespace2025App.Components;
using UnifiedNamespace2025App.Services;
using UnifiedNamespace2025Lib.Models.Database;
using UnifiedNamespace2025Lib.Services;

var builder = WebApplication.CreateBuilder(args);
var conf = builder.Configuration;

builder.Services.AddScoped<HmiJsInterop>();

builder.Services.AddMudServices();
#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Services.AddHostedService<MqttService>();
builder.Services.AddHostedService<TimerServices>();
builder.Services.AddHostedService<VirtualSignalsService>();
builder.Services.AddHostedService<FileSensorService>();

builder.Services.AddSingleton(new DbContextOptionsBuilder<UnifiedNamespaceContext>().UseSqlServer(conf["dbConnectionString"]).Options);
builder.Services.AddKeyedSingleton<IRetainedMessage, EFRetainedMessageService>("retain");

builder.Services.AddKeyedSingleton<MqttClient>("fileSensor");
builder.Services.AddKeyedSingleton<MqttClient>("handleNewTopic");
builder.Services.AddKeyedSingleton<MqttClient>("timerGenerator");
builder.Services.AddKeyedSingleton<MqttClient>("uiTopics");
builder.Services.AddKeyedSingleton<MqttClient>("onTimerToEvaluateVirtualSignals");

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/api/handleNewTopic", async (HttpContext ctx, [FromKeyedServices("handleNewTopic")] MqttClient handleNewTopic, JsonElement element) =>
{
    try
    {
        await handleNewTopic.PublishAsync(
            element.GetProperty("topic").GetString(),
            element.GetProperty("body")
        );
    }
    catch (Exception ex)
    {
        throw;
    }
});

app.Run();
