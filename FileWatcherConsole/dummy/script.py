with open("a.txt", "a") as f:
    lines = [str(i) for i in range(1,100)]
    f.writelines('\n'.join(lines))

with open("b.txt", "a") as f:
    lines = [str(i*2) for i in range(1,100)]
    f.writelines('\n'.join(lines))