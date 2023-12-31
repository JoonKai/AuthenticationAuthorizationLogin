using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AuthenticationAuthorization
{
    //가장 기본적인 로그인 인증 처리 데모
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //로그인 인증처리 서비스 등록
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                #region Menu
                endpoints.MapGet("/", async context =>
                {
                    string content = "<h1>ASP.NET Core 인증과 권한 초간단 코드</h1>";

                    content += "<a href=\"/Login\">로그인</a><br/>";
                    content += "<a href=\"/Login/User\">로그인(User)</a><br/>";
                    content += "<a href=\"/Login/Administrator\">로그인(Administrator)</a><br/>";
                    content += "<a href=\"/Info\">정보</a><br/>";
                    content += "<a href=\"/InfoDetails\">정보(Details)</a><br/>";
                    content += "<a href=\"/InfoJson\">정보(Json)</a><br/>";
                    content += "<a href=\"/Logout\">로그아웃</a><br/>";

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(content);
                }); 
                #endregion
                #region /Login/{username}
                /////////////////////////라우트토큰//////////////////
                endpoints.MapGet("/Login/{Username}", async context =>
                {
                    var username = context.Request.RouteValues["Username"].ToString();
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, username),
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Email, username+"@a.com"),
                        new Claim(ClaimTypes.Role, "Users"),
                        new Claim("원하는 이름", "원하는 값"),
                    };
                    if (username=="Administrator")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Administrators"));
                    }


                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                              claimsPrincipal,
                                              new AuthenticationProperties { IsPersistent = true });//웹브라우저 닫아도 쿠키 남겨놈

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<h3>로그인완료</h3>");
                }); 
                #endregion

                #region Login
                //기본적인 로그인 인증 처리 방법
                endpoints.MapGet("/Login", async context =>
                {
                    var claims = new List<Claim>
                    {
                        //new Claim(ClaimTypes.Name, "User Name")
                        new Claim(ClaimTypes.Name, "아이디")
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<h3>로그인완료</h3>");
                }); 
                #endregion
                #region InfoDetails
                endpoints.MapGet("/InfoDetails", async context =>
                {
                    string result = "";
                    if (context.User.Identity.IsAuthenticated)
                    {
                        result += $"<h3>로그인이름 : {context.User.Identity.Name}</h3>";
                        foreach (var claim in context.User.Claims)
                        {
                            result += $"{claim.Type}= {claim.Value}<br />";
                        }
                        if (context.User.IsInRole("Administrators") && context.User.IsInRole("Users"))
                        {
                            result += "<br />Administrators + Users 권한이 있습니다. <br />";
                        }
                    }
                    else
                    {
                        result += "<h3>로그인하지 않았습니다.</h3>";
                    }

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(result, Encoding.Default);

                });
                #endregion
                #region Info
                endpoints.MapGet("/Info", async context =>
                {
                    string result = "";
                    if (context.User.Identity.IsAuthenticated)
                    {
                        result += $"<h3>로그인이름 : {context.User.Identity.Name}</h3>";
                    }
                    else
                    {
                        result += "<h3>로그인하지 않았습니다.</h3>";
                    }

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(result, Encoding.Default);

                });
                #endregion
                #region InfoJson
                endpoints.MapGet("/InfoJson", async context =>
                {
                    string json = "";
                    if (context.User.Identity.IsAuthenticated)
                    {
                        //Json += "{\"type\":\"Name\",\"value\":\"User Name\"}";
                        var claims = context.User.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value });

                        json += JsonSerializer.Serialize<IEnumerable<ClaimDto>>(
                            claims,
                            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                    }
                    else
                    {
                        json += "{}";
                    }
                    //MIME 타입
                    context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(json);

                });
                #endregion
                #region 로그아웃
                endpoints.MapGet("/Logout", async context =>
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);//로그아웃기능 : 로그인할때 스키마 이름을 그대로 입력한다.

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<h3>로그아웃 완료</h3>");

                });
                #endregion
            });
        }
    }
    /// <summary>
    /// Data Transfer Object
    /// </summary>
    public class ClaimDto
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
