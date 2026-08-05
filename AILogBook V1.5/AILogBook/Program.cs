using AILogBook.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllersWithViews(); // Recommended: Matches your .NET 4.7.2 API format
//builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ChatService>();
builder.Services.AddScoped<ChatService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1440); // Set session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
app.UseSession();
// 2. CRITICAL: This must be the very first line after builder.Build()
//app.UsePathBase("/cloudapp/app36/AIProbeBook");

//// 3. This middleware helps the app recognize the base path correctly
//app.Use((context, next) =>
//{
//    context.Request.PathBase = "/cloudapp/app36/AIProbeBook";
//    return next();
//});

// If we are NOT on your local laptop (Production Server)
if (!app.Environment.IsDevelopment())
{
    app.UsePathBase("/cloudapp/app36/AIProbeBook");
    app.Use((context, next) =>
    {
        context.Request.PathBase = "/cloudapp/app36/AIProbeBook";
        return next();
    });
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Now this will look in the right subfolder for CSS/JS

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "AiChatCustom",
    pattern: "AiChat/Index/{projectKey}/{resId}",
    defaults: new { controller = "AiChat", action = "Index" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Main}/{action=Main}/{id?}");
    //pattern: "{controller=Login}/{action=SignIn}/{id?}");

app.Run();