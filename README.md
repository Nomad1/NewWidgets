*Read this in other languages: [English](README.md), [Русский](README.ru.md)*

# NewWidgets

NewWidgets (temporary name) is a GUI subsystem written with .Net and aimed to be sprite-based and graphics engine independent. As a test example a WinForms renderer is implemented. Support is planned for Unity, MonoGame, RunMobile (own engine), UIKit and other engines among with plain C++ port.

Right now System.Numerics.Vectors is used to work with mathematics and 2D and 3D vectors, but the syntax is compatible with OpenTK, MonoGame and other implementations, so it will be enough to change the headers and `using` directives in corresponding files.

### Porting
Support for third-party engines requires interface implementation for ISprite and the WindowController abstract class. ISprite encapsulates a platform-dependent sprite and provides an interface for its use (position, rotation, scaling, color, transparency, hit-test). All transformations are performed through the auxiliary class Transform, which supports the ascending hierarchy (contains a pointer to the parent Transform). The implementation of WindowControllerBase should contain basic system helper functions - logging, creation of a sprite, screen geometry info. Sample WinForms implementation is in the WinForms directory and the WinFromsSprite and WinFormsController files.

### Integration
To use the library, you need to create an instance of the corresponding WindowController, register the sprites in it and implement following events handling:
- `Key (int code, bool up, char character)` - keyboard action. It supports both special keys and letter-character, separate processing for pressing and releasing;
- `Touch (float x, float y, bool press, bool unpress, int pointer)` - mouse pointer or touchscreen action (MouseUp / MouseDown / MouseMove and similar touchscreen events);
- `Zoom (float x, float y, float value)` - scaling or scrolling action;
- `Draw()` - drawing;
- `Update()` - update animations and internal states (it makes sense to call with a timer at 60fps);

Take a look at the the WinFormsSample TestForm class as an example of thosese events handling. In the case of WinForms during Update() the Invalidate() action for the graphics area is invoked and its OnPaint() event already triggers the Draw() operation. In the case of OpenGL or Direct3D, it makes sense to call Update() in the render cycle, then Draw() and glFlush/SwapBuffers/ID3D11DeviceContext::Flush.

### Usage
While there is no visual editor yet so controls should be created in the code:
```
            WidgetPanel panel = new WidgetPanel (WidgetManager.DefaultWindowStyle);
            panel.Size = new Vector2 (600, 560);
            panel.Scale = WindowControllerBase.Instance.ScreenScale;
            panel.Position = this.Size / 2 - panel.Size * panel.Scale / 2;
            this.AddChild (panel);

            WidgetLabel loginLabel = new WidgetLabel();
            loginLabel.Text = ResourceLoader.Instance.GetString ("login_login");
            loginLabel.Position = new Vector2 (50, 160);
            loginLabel.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.25f;
            panel.AddChild (loginLabel);

```
This may not be not the most convenient way but in general should not be a problem for experienced programmers.

### Architecture
The library uses three layers of abstraction, each of which allows creation of UI with varying complexity for different tasks. However it is advised to use Widget layer instead of raw sprites.

#### ISprite Layer
The base class of sprites contains everything you need to create the simplest interface: it has the ability to display sprites on the screen, do nesting and grouping by using Transform hierarchy, check whether the pointer (mouse or touchscreen) action hits the sprite (HitTest method). Also the sprite can contain several frames allowing animation usage among with Alpha and Color animations.

#### WindowObject Layer
For organized work with the interface there are WindowObject and Window classes with WindowObjectArray collection. They support the full downward hierarchy, containers, OnTouch events, etc. With these objects, you can implement image-based interfaces (ImageObject class), custom components, use text display (LabelObject class). ImageObject and LabelObject are based on sprites. To use LabelObject you'll need a sprite containing needed characters as a frames.

#### Widgets Layer
A set of Widgets components allows you to work at a high level of abstraction, creating buttons, checkboxes, panels and scrolling areas. All components are inherited from WindowObject allowing you to organize an arbitrary structure of components or port interfaces from other systems. It also supports stylesheet system with style inheritance and stored in XML form:
```
 <style name="text_button">
    <back_style>None</back_style>
    <font>default</font>
    <font_size>1.0</font_size>
    <text_color>0xcceeff</text_color>
    <padding>4;2;4;2</padding>
    <hovered_style>text_button_hovered</hovered_style>
  </style>

  <style name="text_button_hovered" parent="default_button_hovered">
    <back_style>None</back_style>
    <text_color>#ffffff</text_color>
  </style>
```
WidgetManager class is used to control styles and performing some global operations alike tooltips and focusing.
Also there is rich-text enabled WidgetText class supporting multi-line texts with automatic line wrapping, color formatting and inline images.

### Problems and Tasks

At the moment, the project has huge list off issues with some of them listed below:
* lack of documentation and examples;
* lack of tests;
* minimal amount of code comments;
* lack of different specific controls: Radio Button, List, ComboBox, Tab Control, etc.;
* architectural flaws;
* poor focus handling;
* (done) problems with style inheritance and their property coverage (resolved in version 1.5);
* support for native clipboard and text selection;
* other unidentified problems;
* lack of community and live projects with NewWidgets;
* stupid name;

Long-term objectives:
* connectors for Unity, MonoGame;
* connectors for native interfaces in iOS (UIKit) and Android;
* C++ port;
* serialization of UI to/from interface files;
* WYSIWYG editor;
* world domination;