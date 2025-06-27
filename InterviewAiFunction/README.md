# Interview GPT (API)

Interview GPT es un aplicativo que te permite usar modelos de inteligencia Artificial para asistir en la captura de información a través de entrevistas. 

El proyecto está dividido en tres repositorios: backend (API), frontend y un AI module.

El API se utiliza para gestionar la información que se almacena en una base de datos SQL. Está construido en C# en una arquitectura de microservicios haciendo uso de la plataforma Azure Functions.

## Tecnologías Utilizadas

- C#
- .dot core 8
- MS SQL
- Azure Active Directory

## Dependencias

### Base de datos

El proyecto fue adaptado para usar una base de datos en MS SQL Server. Se utiliza la librería Entity Framework para la interacción con la base de datos. Sin embargo, no se ha configurado un proceso para hacer migraciones automáticas para los esquemas de datos.

Será requerido crear una base de datos y ejecutar el script de creación de base de datos ubicado en el archivo Database.sql de este repositorio.

### Autenticación

El API dispone de funciones (endpoints) públicas y privdas. Las funciones referentes al módulo administrativo o de gestión son privados. 

Por facilidad, las funciones públicas se encontrarán en los archivos cuyo nombre tiene el prefijo "Public", estas son utilizadas para encuestas que no requiere autenticación e identifican a usuarios y sus sesiones vía un código de invitación.

Las funciones privadas fueron configuradas para usar una autenticación con Azure Active Directory y requieren del envío de un Token JWT validado.


### Envío de correos

El envío de correos es una funcionalidad que se implementó para informar a usuarios que fueron invitados a contenstar una entrevista. Para el envío se ha configurado el uso de servicios externos. Actualmente es soportado [MailGun](https://www.mailgun.com) y [SendGrid](https://www.sendgrid.com). Ver archivos Utils/EmailUtils.cs


## Configuraciones

Para poder ejecutar el código en un ambiente local es preciso contar con un archivo llamado local.settings.json con ubicación en el directorio raíz.

```json
{
  "IsEncrypted": true,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true", //uses azurite on local environment.
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "jwt_audience": "api://[app-id]",
    "jwt_authority": "https://login.microsoftonline.com/[azure-tenant-id]/v2.0",
    "jwt_issuer": "https://sts.windows.net/[azure-tenant-id]/",
    "sqldb_connection": "[connection url]",
    "BASE_URL": "[front-end-base-url]", // utilizada para generación de URLs para envío de correos.
    "MAIL_API_KEY": "[mail-api-key]", // utilizada para la configuración de envío de correos, es soportado únicamente Mailgun y Sendgrid
    "MAIL_SERVICE": "[mailgun|sendgrid]"
  },
  "ConnectionStrings": {
    "SQLConnectionString": {
      "ConnectionString": "",
      "ProviderName": ""
    }
  }
}
```

## Licencia

Todo el material distruido por este repositorio tiene licencia MIT, más detalles en LICENSE.md

## Contribuidores

- José Daniel Zárate Martínez
- Raúl Ignacio Cerrato
- Alejandra Pérez Ortega
- Miluska Pajuelo
