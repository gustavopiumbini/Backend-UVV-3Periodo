# 📦 DropMon - Admin Dashboard

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET Core](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![SQLite](https://img.shields.io/badge/sqlite-%2307405e.svg?style=for-the-badge&logo=sqlite&logoColor=white)
![JavaScript](https://img.shields.io/badge/javascript-%23323330.svg?style=for-the-badge&logo=javascript&logoColor=%23F7DF1E)

## 🚀 Overview

**DropMon** is a project made for studying, it is a Full-Stack SaaS application built from scratch to manage exclusive streetwear drops, specifically designed as the administrative panel for the **tsey** brand. 

This project demonstrates a complete end-to-end data flow: from an interactive web client to a robust C# API, ultimately persisting data securely in a relational database.

## ✨ Features

- **Product Registration:** Add new clothing items specifying name, category, price, and stock quantity.
- **Exclusive Drop Tagging:** Boolean toggle to mark specific items as highly anticipated drops.
- **Real-time Synchronization:** Instant UI updates upon successful database transactions.
- **RESTful API Consumption:** The frontend communicates seamlessly with the backend using the asynchronous Fetch API.

## 🛠️ Tech Stack

### Backend (Web API)
- **Language & Framework:** C# 10, ASP.NET Core
- **Database:** SQLite
- **ORM:** Entity Framework Core (Code-First approach)
- **Architecture:** MVC (Controllers and Models mapped via attributes)

### Frontend (Client)
- **Markup & Styling:** HTML5, CSS3 (Dark mode UI/UX)
- **Logic:** Vanilla JavaScript
- **Integration:** Fetch API for HTTP requests (`GET`, `POST`)

## ⚙️ How to Run Locally

### 1. Start the API (Backend)
Navigate to the API folder and run the .NET server:
```bash
cd DropMonAPI
dotnet run

The API will typically start at http://localhost:5126. 
```

### 2. Launch the Dashboard (Frontend)
Ensure the API is running, then simply open the index.html file in any modern web browser.

Note: The API is configured with a CORS policy to accept incoming requests from the local client.

##

### 🧠 Core Engineering Concepts Applied
**Model Binding & Deserialization**: Automatic conversion of JSON payloads into C# Objects.

**Asynchronous Programming**: Utilizing async/await and Task in C# to ensure non-blocking database operations.

**CORS Management**: Configuring Cross-Origin Resource Sharing policies to secure API endpoints.

**Database Migrations**: Versioning the database schema via EF Core CLI (dotnet ef).
