import torch
import torch.onnx

class SimpleNPC(torch.nn.Module):
    def forward(self, x):
        return torch.sigmoid(x[:, 0])  # Реакция на rep

model = SimpleNPC()
dummy = torch.randn(1, 2)
torch.onnx.export(model, dummy, "npc_model.onnx")