
using EFAPIMavel.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EFAPIMavel
{
    public class Program
    {
        //static DbApiContext db = new DbApiContext();
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<DbApiContext>(options =>
            options.UseSqlServer("Server=VCECPELITPC2;Initial Catalog=dbAPI;Integrated Security=True;Encrypt=False;"));
            
            #region Set up
            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<DbApiContext>()
            .AddDefaultTokenProviders();
            #endregion
            builder.Services.AddAuthorization();
            builder.Services.AddEndpointsApiExplorer();

            #region Swagger add bearer to top
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Marvel API", Version = "v1" });

                // Add JWT Authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your valid token in the text input below.\n\nExample: \" abcdef12345\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            #endregion

            var app = builder.Build();
            #region Create roles
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                await EnsureRolesAsync(roleManager);
            }
            #endregion
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            // GET endpoints
            app.MapGet("/users", () =>
            {
                DbApiContext db = new DbApiContext();
                return db.TblAvengers.ToList();
                db.Dispose();
            })
               .WithName("GetUsers")
               .WithOpenApi();

            app.MapGet("/contacts", () => {
                DbApiContext db = new DbApiContext();
                return db.TblContacts.ToList();
                db.Dispose();

            })
               .WithName("GetContacts")
               .WithOpenApi();
            // POST endpoints
            app.MapPost("/users", (TblAvenger newUser) =>
            {
                DbApiContext db = new DbApiContext();
                db.TblAvengers.Add(newUser);
                db.SaveChanges();
                db.Dispose();
                return Results.Created($"/users/{newUser.Username}", newUser);
            }).WithName("CreateUser").WithOpenApi();

            app.MapPost("/contacts", (TblContact newContact) =>
            {
                DbApiContext db = new DbApiContext();
                db.TblContacts.Add(newContact);
                db.SaveChanges();
                db.Dispose();
                return Results.Created($"/contacts/{newContact.HeroName}", newContact);
            }).WithName("CreateContact").WithOpenApi();
            // PUT endpoints
            app.MapPut("/users/{username}", (string username, TblAvenger updatedUser) =>
            {
                DbApiContext db = new DbApiContext();
                var user = db.TblAvengers.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    user.Password = updatedUser.Password; // Update necessary fields
                    db.SaveChanges();
                    db.Dispose();
                    return Results.NoContent();
                }
                return Results.NotFound();
            }).WithName("UpdateUser").WithOpenApi();

            app.MapPut("/contacts/{id}", (int id, TblContact updatedContact) =>
            {
                DbApiContext db = new DbApiContext();
                var contact = db.TblContacts.FirstOrDefault(c => c.AvengerId == id);
                if (contact != null)
                {
                    contact.HeroName = updatedContact.HeroName; // Update necessary fields
                    contact.RealName = updatedContact.RealName;
                    db.SaveChanges();
                    db.Dispose();
                    return Results.NoContent();
                }
                return Results.NotFound();
            }).WithName("UpdateContact").WithOpenApi();
            // DELETE endpoints
            app.MapDelete("/users/{username}", (string username) =>
            {
                try
                {
                    DbApiContext db = new DbApiContext();
                    var user = db.TblAvengers.FirstOrDefault(u => u.Username == username);
                    if (user != null)
                    {
                        db.TblAvengers.Remove(user);
                        db.SaveChanges();
                        db.Dispose();
                        return Results.NoContent();
                    }
                }
                catch(Exception e)
                {
                    return Results.Conflict();
                }
                return Results.NotFound();
            }).WithName("DeleteUser").WithOpenApi();

            app.MapDelete("/contacts/{id}", (int id) =>
            {
                DbApiContext db = new DbApiContext();
                var contact = db.TblContacts.FirstOrDefault(c => c.AvengerId == id);
                if (contact != null)
                {
                    db.TblContacts.Remove(contact);
                    db.SaveChanges();
                    db.Dispose();
                    return Results.NoContent();
                }
                return Results.NotFound();
            }).WithName("DeleteContact").WithOpenApi();

            // Filter example
            app.MapGet("/users/contacts/{username}", (string username) =>
            {
                DbApiContext db = new DbApiContext();
                var result = db.TblContacts.Where(c => c.Username == username).ToList();
                return result.Any() ? Results.Ok(result) : Results.NotFound();
                db.Dispose();
            }).WithName("GetContactsByUser").WithOpenApi();
            app.MapGet("/users/{username}", (string username) =>
            {
                DbApiContext db = new DbApiContext();
                var user = db.TblAvengers.FirstOrDefault(u => u.Username == username);

                if (user != null)
                {
                    db.Dispose();
                    return Results.Ok(user); // Return 200 OK with the user
                }

                db.Dispose();
                return Results.NotFound(); // Return 404 if not found
            }).WithName("GetUserByUsername").WithOpenApi();

            app.Run();
        }
        #region Generate JWT Token with user rights
        private static string GenerateJwtToken(IdentityUser user, UserManager<IdentityUser> userManager)
        {
            var userRoles = userManager.GetRolesAsync(user).Result;
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("ehp8LjNkF58gRJED7jszQYvmE+nrOSmicxpspxNoPV8="));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "MavelAPI",
                audience: "MavelAPI",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
        #region Add user roles to DB
        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
        #endregion

    }
}
