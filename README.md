## About

A sample app to show information of places around you using Geospatial API, overlaying on the camera image.
Based on the sample of Geospatial API.

## Versions

Unity 2019.4
ARFoundation 4.1.5
ARCore Extension 1.33.0

## Gradle settings

For Unity 2019.4, you need to use gradle v6.5.1 (or something) to build for Android.
You can change the gradle version via Preferences -> External Tools

## API Key

You need Google Cloud API Key with these capabilities enabled.

* ARCore API
* Places API

Put the API Key string to these locations.

* Project Settings -> XR Plug-in Management -> ARCore Extensions
  * Android API Key
  * iOS API Key
* googlePlacesApiKey in PlacesController.cs

## Cautions about billing

This program may trigger many Google Places API requests.
Be prepared that you may be charged more than you think.
Especially if you build and distribute to many people, and/or use it everyday.


