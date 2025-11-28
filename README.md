# CourseManagement

## Introduction
CourseManagement is a service for managing teachers and educational courses.
The system provides operations for user registration, authentication, retrieving the list of teachers, and creating courses.

## Installation
Follow the steps below to install and run the application.

1. Clone the repo
   ```
   git clone https://github.com/username/course-management.git
   ```
3. Navigate into project folder
    ```
    cd CourseManagement
    ```
4. Start required services
    ```
    docker compose up -d
    ```
5. Open Swagger UI
    ```
    http://localhost:8080/swagger
    ```
## REST API
The REST API to the example app is described below.
### [1] Register
#### Request
`POST /api/v2/auth/register`
```
curl -X POST \
  'http://localhost:8080/api/v2/auth/register' \
  -H 'accept: application/json' \
  -H 'IdempotencyKey: 00000000-0000-0000-0000-000000000000' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "user",
    "password": "user",
    "lastName": "ivanov",
    "firstName": "ivan",
    "middleName": "ivanovich"
  }'
```
#### Response
```
{
  "id": "<TEACHER_ID>",
  "login": "user",
  "lastName": "ivanov",
  "firstName": "ivan",
  "middleName": "ivanovich"
}
```

### [2] Login
#### Request
`POST /api/v2/auth/login`
```
curl -X POST \
  'http://localhost:8080/api/v2/auth/login' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "user",
    "password": "user"
  }'
```
#### Response body
```
{
  "access": "<YOUR_ACCESS_TOKEN>"
}
```

### [3] Get list of Teachers
#### Request
`GET /api/v2/teachers`
```
curl -X GET \
  'http://localhost:8080/api/v2/teachers?fields=id,login,firstname&pageNumber=1&pageSize=10' \
  -H 'accept: application/json' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```
#### Response body
```
[
  {
    "id": "<TEACHER_ID>",
    "login": "user",
    "firstName": "ivan"
  }
]
```

### [4] Create course
#### Request
`POST /api/v2/courses`
```
curl -X POST \
  'http://localhost:8080/api/v2/courses' \
  -H 'accept: application/json' \
  -H 'IdempotencyKey: 00000000-0000-0000-0000-000000000001' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>' \
  -H 'Content-Type: application/json' \
  -d '{
    "teacherId": "<TEACHER_ID>",
    "title": "math",
    "description": "mathematics for schoolchildren"
  }'
```
#### Response body
```
{
  "id": "<COURSE_ID>",
  "title": "math",
  "description": "mathematics for schoolchildren",
  "createdAt": "<ISO_TIMESTAMP>",
  "teacherId": "<TEACHER_ID>"
}
```

### [5] Get course by ID
#### Request
`GET /api/v2/courses/{id}`
```
curl -X GET \
  'http://localhost:8080/api/v2/courses/<COURSE_ID>?fields=id,title,description' \
  -H 'accept: application/json' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```
#### Response body
```
{
  "statusCode": 429,
  "message": "Too many requests. Please try again later."
}
```
#### Response headers
```
retry-after: 10
x-limit-remaining: 0
```
