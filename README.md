# Simple Function App Demo

This is a simple function app used for demonstration of deployment to azure via ARM template from a GitHub URL, or via direct deployment from Azure using a GitHub url.

Either create the ARM template and leverage the link, or use the Deployment center in your Azure Function app to point to the public URL for this repo.  

```https
https://github.com/blgorman/SimpleFunctionAppDemo.git
```  

>**Note:** that's not the same repo!

## Quick Info

The following information may be useful to aid in your demonstrations

- There are two functions
  - Function1 (Anonymous access, the default function generated in a new Azure Function - pass your name in the query string or body)
  - GetPeople (A function that requires a function key along with the URL to return data.  The data is prefabricated with eight people. The people are four superheros, two Smiths and two Johnsons - you can filter the results by name of the person, such as `smith`).

## A postman collection

In order to easily query this function app, import the Postman collection that is included.  Also import the environment variables and update the variables to match your deployment.
