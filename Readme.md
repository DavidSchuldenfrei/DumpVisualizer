# Dump Visualizer

Dump Visualizer is a visualizer for any object in Visual Studio 2019.

The source object to view is serialized as an HTML string and then shown in a browser window. The Html format is inspired by the `Dump` method of [Linqpad](http://linqpad.net).

As a visualizer needs to be registered for a specific object type, and you cannot register a visualizer for `object`, this visualizer is registered for `System.WeakReference`.

## Usage: 

### Install
compile the project. Copy the resulting Dlls in the Visualizer folder of Visual Studio.

### Viewing
To view the vale of a variable `v`, add to the Watch Window the value `new System.WeakReference(v)`. Choose "Dump Visualizer" as a viewer. If you want to keep track of the values of this variable, you can choose to "View in a Browser" to have this open in your prefered browser. 

