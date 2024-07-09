import pandas as pd
import os
import re
import pprint
from matplotlib import pyplot as plt
def skip_until(f, string: str):
    for l in f:
        if string in l:
            break

def read_until(f, string: str):
    output = ""
    for line in f:
        if string not in line:
            output += line
        else:
            break
    return output



class StageResults:

    def __init__(self, num, enem_by_type: dict, decision_results, num_habs):
        self.num = num
        self.enem_by_type = enem_by_type
        self.decision_results = decision_results
        self.num_habs = num_habs
        self.num_enemies = sum(map(lambda p: int(p), enem_by_type.values()))

    def __repr__(self):
        return f"Stage {self.num}\nEnemy by Type: {self.enem_by_type}\nDecision Results: {self.decision_results}\nNumber of rooms: {self.num_habs}\nNumber of Enemies: {self.num_enemies}"

    def extract_info_as_dictionary(self):
        d = {'stage': self.num, "Number of rooms": self.num_habs, "Number of enemies": self.num_enemies}
        d.update(self.enem_by_type.items())
        dicts_by_alg = []

        for alg, results in self.decision_results.items():
            dalg = dict(d)
            dalg.update({"Algoritmo": alg})
            dalg.update(results.items())
            dicts_by_alg.append(dalg)
        return dicts_by_alg


hp_col_name = "HP"
dmg_col_name = "Daño"
num_ene_col_name = "Num enemies by stage"
num_room_col_name = "Num rooms"
result_list = []
global_result_list = []

conversion_dict = {
    "EAGER_ASTAR": "Voraz A*",
    "EAGER": "Voraz",
    "BEHAVIOR_TREE": "Árbol de Comportamiento",
    "DECISION_TREE": "Árbol de Decisión"
}
if os.path.exists("resultados"):
    os.chdir("resultados")
    for archivo in os.listdir():
        f = open(archivo, "r")
        skip_until(f, "config params")
        num_stages = int(re.search('\d+',f.readline()).group()) # Obten numstages
        num_enemies = int(re.search('\d+',f.readline()).group())
        num_rooms = int(re.search('\d+',f.readline()).group())
        skip_until(f, "Accumulative end results")

        global_results = read_until(f, "STAGE")

        for match in re.findall(r'\(([^)]+)\) Score: (\d+) turnos: (\d+) hp: (\d+)', global_results):
            decision_type = conversion_dict[match[0]]
            score = int(match[1])
            turnos = int(match[2])
            hp = int(match[3])
            global_result_list.append({
                num_room_col_name: num_rooms,
                num_ene_col_name: num_enemies,
                "Score": score,
                "Turnos": turnos,
                hp_col_name: hp,
                "Algoritmo":decision_type
            })
        stages = []
        result_list.append(stages)

        for stage in range(num_stages):
            text = read_until(f, "STAGE")
            enemigos = dict(re.findall(r'([A-Z_]+):\s*(\d+)', text))
            decision_data = {}
            for match in  re.findall(r'\(([^)]+)\) Score: (\d+) turnos: (\d+) hp: (\d+)', text):
                decision_type = match[0]
                score = match[1]
                turnos = match[2]
                hp = match[3]
                decision_data[decision_type] = {
                    "Score": score,
                    "Turnos": turnos,
                    "HP": hp
                }
            stages.append(StageResults(stage, enemigos, decision_data, num_rooms))


res = []
for test in result_list:
    for stage in test:
        res.extend(stage.extract_info_as_dictionary())

df = pd.DataFrame(res)

df_global = pd.DataFrame(global_result_list)
df_global[dmg_col_name] = 5E5 - df_global[hp_col_name]
print(df_global)

# Gráficas de daños para 5 y 10 enemigos

df_low_5 = df_global.loc[df_global[num_ene_col_name] == 5]

df_low_10 = df_global.loc[df_global[num_ene_col_name] == 10]

fig, axs = plt.subplots(1, 2, figsize=(12, 6), sharex=True, sharey=True)


# Subgráfica daño para 5 enemigos
df_low_5.pivot(index=[num_room_col_name],
               columns="Algoritmo",
               values=dmg_col_name).plot(ax=axs[0])

# Gráfica daño para 10 enemigos
df_low_10.pivot(index=[num_room_col_name],
                columns="Algoritmo",
                values=dmg_col_name).plot(ax=axs[1])
axs[0].title.set_text('NºEnemigos = 5')
axs[0].set_ylabel("")
axs[0].set_xlabel("")
axs[1].title.set_text('Nº Enemigos = 10')
axs[1].set_ylabel("")
axs[1].set_xlabel("")
plt.suptitle("Resultados daño al jugador con pocos enemigos", fontsize=16)
fig.text(0.5, 0.03, "Número de habitaciones", ha='center', fontsize=12)
fig.text(0.03, 0.5, "Daño causado al jugador", va='center', rotation='vertical', fontsize=12)

plt.subplots_adjust(left=0.1, right=0.9, top=0.9, bottom=0.15)

plt.show()

# HP restante empleados por los algoritmos con muchos enemigos

df_enemies =  df_global.loc[df_global[num_ene_col_name] > 30]
df_enemies.pivot(index=[num_ene_col_name, num_room_col_name],
                            columns="Algoritmo",
                            values="HP").plot(ylim=(2E5, 5E5),
                                              title="Resultados hp restante del jugador con multiples enemigos")
plt.ylabel("Puntos de salud restante del jugador")
plt.xlabel("(Nº Enemigos, Nº Habitaciones)")

xt = list(map(lambda x: str(x), df_enemies.pivot(index=[num_ene_col_name, num_room_col_name],columns="Algoritmo").axes[0].values))
plt.xticks(range(len(xt)), xt, rotation=90)
plt.show()


# Gráfica turnos empleados por los algoritmos con muchos enemigos

df_turnos = df_global.loc[df_global[num_ene_col_name] > 30]

df_turnos.pivot(index=[num_ene_col_name, num_room_col_name],
                            columns="Algoritmo",
                            values="Turnos").plot(title="Turnos empleados por los algoritmos > 35 enemigos")
plt.ylabel("Número de turnos")
plt.xlabel("(Nº Enemigos, Nº Habitaciones)")
plt.xticks(range(len(xt)), xt, rotation=90)

plt.show()


# Gráfica turnos empleados por los algoritmos con pocos enemigos

df_turnos = df_global.loc[df_global[num_ene_col_name] < 15]
df_turnos.pivot(index=[num_ene_col_name, num_room_col_name],
                            columns="Algoritmo",
                            values="Turnos").plot(title="Turnos empleados por los algoritmos <= 10 enemigos")
plt.ylabel("Número de turnos")
plt.xlabel("(Nº Enemigos, Nº Habitaciones)")
xt = list(map(lambda x: str(x), df_turnos.pivot(index=[num_ene_col_name, num_room_col_name],columns="Algoritmo").axes[0].values))
plt.xticks(range(len(xt)), xt, rotation=90)
plt.show()
plt.show()
