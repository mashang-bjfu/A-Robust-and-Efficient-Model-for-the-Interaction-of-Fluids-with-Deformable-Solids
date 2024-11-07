# Interaction of Fluids with Deformable Solids based on Spatiotemporal Consistency
The code in this repository is the implementation code of the paper "Interaction of Fluids with Deformable Solids based on Spatiotemporal Consistency" in "The Visual Computer"
The following describes the functions of important scripts：
FluidMain script: Configure the relevant data of water simulation, call PBFSolver to implement PBF method to simulate the movement of water.
PBFSolver: Calculates the flow of water, the collision effect of water and solid. (Volume map information is needed to calculate collisions)
ModelMain: Records all the information of the solid, including vertex information, triangle index, normal vector, node information, SDF and volume map, etc.
ModelsMain: Using the data in ModelMain, call BoundaryMap to update SDF and volume map information.
BoundaryMap: Updates SDF and volume map information.
