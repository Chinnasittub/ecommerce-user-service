FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ✅ copy โฟลเดอร์ shared-library และ user-service จาก context root
COPY shared-library ./shared-library
COPY user-service/. .

RUN dotnet restore "UserService.csproj"

# 🔥 สำคัญ: ลบไฟล์ AssemblyInfo อัตโนมัติ ที่ dotnet สร้างให้
RUN rm -rf ./obj ./shared-library/obj

RUN dotnet publish "UserService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]
