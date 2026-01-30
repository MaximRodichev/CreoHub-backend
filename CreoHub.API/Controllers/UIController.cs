using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("[controller]")]
public class UIController : ControllerBase
{
    [HttpGet("auth")]
    public IActionResult Auth()
    {
        // Ваша ссылка на метод логина Google, который мы создали ранее
        var googleLoginUrl = "/api/account/auth/google-signin"; 

        var html = $@"
        <!DOCTYPE html>
        <html lang='ru'>
        <head>
            <meta charset='UTF-8'>
            <title>Авторизация Gmarket</title>
            <style>
                body {{ font-family: sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; background: #f4f4f9; }}
                .card {{ background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); text-align: center; }}
                .btn-google {{ 
                    display: inline-block; background: #4285F4; color: white; padding: 10px 20px; 
                    text-decoration: none; border-radius: 4px; font-weight: bold; margin-top: 20px;
                }}
                .btn-google:hover {{ background: #357ae8; }}
            </style>
        </head>
        <body>
            <div class='card'>
                <h1>Добро пожаловать</h1>
                <p>Выберите способ входа в Gmarket:</p>
                <a href='{googleLoginUrl}' class='btn-google'>Войти через Google</a>
            </div>
        </body>
        </html>";

        return Content(html, "text/html", System.Text.Encoding.UTF8);
    }
}