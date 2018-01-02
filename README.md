# AdVdGraphs
Tool to draw graphs in Unity. This has not been fully tested and crashes/memory leaks may occur, use at your own risk.

![Alt Text](https://raw.githubusercontent.com/AdVdTools/AdVdGraphs/master/Demo.gif)

## Setup
- Create a Graph asset on the create asset menu.
- Drag the Graph to the GraphViewer window, which can be opened from the window menu.
- Call Graph.AddData("graph name", value) from your scripts to log a value over time.
- It is also possible to find a graph using Graph.FindGraph("graph name") or any other means and then call AddData(value) on the instance.

![Alt Text](https://raw.githubusercontent.com/AdVdTools/AdVdGraphs/master/setup.gif)

## Configuration
![Alt Text](https://raw.githubusercontent.com/AdVdTools/AdVdGraphs/master/graph_configuration.gif)

- Set the color and draw mode.
- Adjust the offset and scale to draw the graph.
- Set whether the data should be reset on play (only the graphs in the viewer are affected).
- Set the amount of data that a graph can store. Old data is overwritten if the array data is full.
- Configure the appearance of the markers for the Points draw mode (texture and size).
- Export and import graph data as csv files.

## GraphViewer
![Alt Text](https://raw.githubusercontent.com/AdVdTools/AdVdGraphs/master/graph_viewer_controls.gif)

- Set the order in which the graphs are drawn in the viewer.
- Add graphs to the viewer by dragging the asset to the list or through the add (+) options: create a new graph asset or choose existing graphs with the object picker.
- Remove the selected graph from the viewer by clicking the delete button (-) or the Del/Supr key.
- Clear the data from the graphs in the viewer using the buttons in the top left.
- Toggle whether the view should follow the data as it is added in either axis. The view window will expand until the maximum view size is reached.
- Open the Settings Inspector to configure more options, such as the default marker texture and maximum view size.
- Hold the right or wheel mouse buttons and drag to move the view around.
- Use the mouse wheel to zoom in and out. Hold Ctrl/Cmd or Shift to zoom the axis separately.
