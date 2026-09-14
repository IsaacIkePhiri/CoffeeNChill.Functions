CoffeeNChill Part 1 – Setup & Running

CoffeeNChill Part 1 is a .NET 8 Azure Functions API that provides menu management and document management functionality. The project uses Azure Table Storage for menu data and Azure File Storage for staff documents.

Requirements

Before running the project, install:

* Visual Studio 2022 with .NET 8 support
* .NET 8 SDK
* Azure Functions Core Tools v4
* Docker Desktop
* Git
* Postman

You will also need access to the team’s Azure Storage account for the document functionality.

Setup

Clone the CoffeeNChill repository from GitHub and open the project in Visual Studio.

The project uses Azurite through Docker for local Azure Storage development. Start Docker Desktop and make sure the coffeechill-Azurite container is running:

docker start coffeechill-Azurite

If the container does not exist, create it with:

docker run -d --name coffeechill-Azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite

Create local.settings.json by copying local.settings.example.json. Configure the required storage settings, including the team’s Azure Storage connection for the staff-docs File Share. The FUNCTIONS_WORKER_RUNTIME value must be set to dotnet-isolated.

Running the API

Open PowerShell in the CoffeeNChill project directory and run:

func start

The API will run locally at:

http://localhost:7071

Keep the terminal running while testing the API.

API Functionality

Menu API

* POST /api/menu – Create a menu item
* GET /api/menu – Retrieve all menu items
* GET /api/menu/category/{category} – Retrieve menu items by category
* PUT /api/menu/{category}/{id} – Update a menu item
* DELETE /api/menu/{category}/{id} – Delete a menu item

Document API

* POST /api/documents/upload – Upload a document
* GET /api/documents – List uploaded documents
* GET /api/documents/download/{fileName} – Download a document

Testing

The included CoffeeNChill.postman_collection.json contains the Part 1 API requests and can be imported into Postman for testing.

The application uses the following storage setup:

CoffeeNChill API
      │
      ├── Menu API
      │      └── Azurite → Local Table Storage
      │
      └── Document API
             └── Azure Storage → staff-docs File Share

Docker Image

A Docker image is also available as:

isaacphiri/coffeechill-functions:v1.0

The Docker image can be pulled with:

docker pull isaacphiri/coffeechill-functions:v1.0

The project can therefore be run locally using Azure Functions Core Tools, with Docker/Azurite providing local storage and Azure Storage providing the document File Share.
