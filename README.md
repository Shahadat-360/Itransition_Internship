# FormsApp - ASP.NET MVC Forms Application

A robust forms management application built with ASP.NET MVC that allows users to create, share, and collect responses to customizable forms.

## Features

- **User Authentication**: Secure registration and login system
- **Form Management**: Create, edit, delete, and duplicate forms
- **Question Types**: Support for various question types (text, multiple choice, polls, etc.)
- **Access Control**: Public forms and private forms with specific user access
- **Tagging System**: Organize forms with tags and topics
- **Admin Panel**: User management with admin capabilities
- **Search Functionality**: Find forms by title, description, or tags
- **Responsive Design**: Modern UI with Bootstrap and Bootstrap Icons

## Getting Started

### Prerequisites

- .NET 9.0 or higher
- SQL Server (local or remote)
- Visual Studio 2022 or any compatible IDE

### Installation

1. Clone the repository
```
git clone https://github.com/yourusername/FormsApp.git
```

2. Open the solution in Visual Studio

3. Update the connection string in `appsettings.json` to point to your database

4. Run the application
```
dotnet run
```

### Initial Admin Setup

The application uses a built-in mechanism to initialize the admin user on first run:

1. **Using appsettings.json (Recommended for Development)**
   
   The application will automatically create an admin user using credentials from `appsettings.json`:
   ```json
   "AdminCredentials": {
     "Email": "admin@formsapp.com",
     "Password": "Admin123!"
   }
   ```

2. **For Production Deployment**

   - Create a custom admin user by updating the AdminCredentials section in your production appsettings.json
   - This will only be used if no admin user exists in the database
   - For security, consider changing the default password immediately after first login

## Recent Updates

- **Admin Panel Enhancements**: Improved user management with bulk actions
- **Notification System**: Fixed duplicate notification display
- **Tag Management**: Added cleanup for unused tags
- **Question Validation**: Made Description field optional
- **Admin Interface**: Updated with icon-only buttons and improved tooltips
- **Form Display**: Enhanced clickable rows and improved search functionality

## Architecture

- **MVC Pattern**: Clear separation of Models, Views, and Controllers
- **Entity Framework Core**: ORM for database interactions
- **Identity Framework**: User authentication and authorization
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: For loose coupling and testability

## Database Schema

The application uses several key entities:
- Users (ApplicationUser)
- FormTemplates
- Questions
- QuestionOptions
- FormResponses
- Answers
- Tags
- Topics

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Bootstrap for UI components
- Bootstrap Icons for icon set
- ASP.NET Core team for the excellent framework 