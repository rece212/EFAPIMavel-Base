 Additional Migration for Schema Changes

If there are any additional schema changes required, you can add them as follows:

```bash
dotnet ef migrations add AdditionalSchemaChanges -c DbApiContext
dotnet ef database update -c DbApiContext
```
