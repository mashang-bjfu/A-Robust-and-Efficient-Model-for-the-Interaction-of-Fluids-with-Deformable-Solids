# A Robust and Efficient Model for the Interaction of Fluids with Deformable Solids
The code in this repository provides data support for the paper "A Robust and Efficient Model for the Interaction of Fluids with Deformable Solids", which has been submitted to the journal "The Visual Computer" (under review).
<br>The following describes the functions of important scripts：
<br>FluidMain script: Configure the relevant data of water simulation, call PBFSolver to implement PBF method to simulate the movement of water.
<br>PBFSolver: Calculates the flow of water, the collision effect of water and solid. (Volume map information is needed to calculate collisions)
<br>ModelMain: Records all the information of the solid, including vertex information, triangle index, normal vector, node information, SDF and volume map, etc.
<br>ModelsMain: Using the data in ModelMain, call BoundaryMap to update SDF and volume map information.
<br>BoundaryMap: Updates SDF and volume map information.
