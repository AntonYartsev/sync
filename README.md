# Sync

A real-time collaborative code editor built with Blazor.

## Features

- Collaborative editing with multiple users
- Syntax highlighting for various languages
- Real-time updates via WebSockets
- Session-based collaboration
- Language selection (C#, JavaScript, TypeScript, HTML, CSS, Java, Python, JSON)

## Getting Started

### Prerequisites

- .NET 7.0 or higher
- Docker (optional)

### Running Locally

```bash
# Clone the repository
git clone https://github.com/antonyartsev/sync.git
cd Sync

# Run the application
dotnet run --project Sync.Mono
```

The application will be available at `https://localhost:7080` or `http://localhost:5153`.

### Using Docker

```bash
# Build and run using Docker Compose
docker-compose up
```

## Usage

1. Open the application in your browser
2. Create a new session by clicking "Create session"
3. Share the URL with collaborators
4. Start editing code together in real-time
5. Select your preferred language from the dropdown menu

## License

MIT 