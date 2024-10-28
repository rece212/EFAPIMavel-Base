
using EFAPIMavel.Models;

namespace EFAPIMavel
{
    public class Program
    {
        //static DbApiContext db = new DbApiContext();
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

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
    }
}
