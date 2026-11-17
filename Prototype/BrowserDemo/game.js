const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');

const abilityListEl = document.getElementById('abilityList');
const questListEl = document.getElementById('questList');
const playerLevelEl = document.getElementById('playerLevel');
const implantSlotsEl = document.getElementById('implantSlots');
const creditsEl = document.getElementById('credits');
const healthBarEl = document.getElementById('healthBar');
const ghostBarEl = document.getElementById('ghostBar');
const bioBarEl = document.getElementById('bioBar');
const xpBarEl = document.getElementById('xpBar');

const WORLD = { width: 1800, height: 1200 };
const keys = {};
const pointer = { x: 0, y: 0, down: false };

const abilityMeta = [
  { key: 'ballistics', label: 'Ballistics & Arms' },
  { key: 'bioSync', label: 'Bio-Sync' },
  { key: 'cyberTech', label: 'Cyber-Tech' },
  { key: 'tactics', label: 'Tactics & Reflex' },
  { key: 'grit', label: 'Grit & Physique' }
];

const player = {
  x: 170,
  y: 170,
  radius: 18,
  speed: 210,
  facing: 0,
  hp: 100,
  maxHp: 100,
  bioCalories: 100,
  maxBioCalories: 100,
  ghostBleed: 20,
  maxGhostBleed: 100,
  xp: 0,
  xpToNext: 60,
  level: 1,
  credits: 120,
  skillPoints: 2,
  implantSlots: 2,
  fireCooldown: 0,
  tacticalFocus: 0,
  tacticalReady: true,
  bioBoostTimer: 0,
  stats: {
    ballistics: 12,
    bioSync: 10,
    cyberTech: 8,
    tactics: 11,
    grit: 12
  }
};

const camera = { x: 0, y: 0 };
const bullets = [];
const enemies = [];
const pickups = [];
const walls = [
  { x: 260, y: 130, w: 180, h: 140 },
  { x: 620, y: 250, w: 220, h: 120 },
  { x: 1020, y: 200, w: 180, h: 185 },
  { x: 520, y: 720, w: 210, h: 170 },
  { x: 980, y: 730, w: 220, h: 180 },
  { x: 1350, y: 520, w: 200, h: 100 }
];

const questState = [
  { id: 'scrapyard', title: 'Scrapyard Sweep', description: 'Eliminate 6 raiders on the outer junk line.', target: 6, progress: 0, complete: false },
  { id: 'fuel', title: 'Fuel Cell Recovery', description: 'Recover 3 fuel cells scattered around Rusthollow.', target: 3, progress: 0, complete: false },
  { id: 'hold', title: 'Hold the Line', description: 'Survive a 25-second ambush while the Silt whips up.', target: 25, progress: 0, complete: false }
];

let waveTimer = 0;
let lastTime = performance.now();
let raiderKills = 0;
let survivalActive = false;
let survivalSeconds = 0;

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function dist(aX, aY, bX, bY) {
  const dx = bX - aX;
  const dy = bY - aY;
  return Math.hypot(dx, dy);
}

function randomBetween(min, max) {
  return min + Math.random() * (max - min);
}

function addPickup(x, y, kind = 'fuel') {
  pickups.push({ x, y, radius: 12, kind, picked: false });
}

function spawnEnemy(x, y, kind = 'raider') {
  const archetypes = {
    raider: { speed: 90, hp: 42, dmg: 10, color: '#ff7a7a', radius: 16 },
    brute: { speed: 62, hp: 80, dmg: 16, color: '#fca761', radius: 20 },
    skirmisher: { speed: 110, hp: 32, dmg: 8, color: '#8bd1ff', radius: 15 }
  };

  const data = archetypes[kind] || archetypes.raider;
  enemies.push({
    x,
    y,
    radius: data.radius,
    speed: data.speed,
    hp: data.hp,
    maxHp: data.hp,
    damage: data.dmg,
    color: data.color,
    kind,
    attackCooldown: randomBetween(0.7, 1.5),
    reward: kind === 'brute' ? 30 : 18
  });
}

function spawnWave() {
  const spawnCount = 6;
  for (let i = 0; i < spawnCount; i += 1) {
    const side = Math.floor(Math.random() * 4);
    let x = Math.random() * WORLD.width;
    let y = Math.random() * WORLD.height;

    if (side === 0) { x = -20; }
    if (side === 1) { x = WORLD.width + 20; }
    if (side === 2) { y = -20; }
    if (side === 3) { y = WORLD.height + 20; }

    spawnEnemy(x, y, i % 3 === 0 ? 'brute' : 'raider');
  }
}

function addFuelCells() {
  const nodes = [
    { x: 350, y: 340 },
    { x: 780, y: 300 },
    { x: 1180, y: 870 },
    { x: 1460, y: 620 },
    { x: 660, y: 930 }
  ];
  nodes.forEach((node) => addPickup(node.x, node.y, 'fuel'));
}

function updateHud() {
  const hpPct = (player.hp / player.maxHp) * 100;
  const ghostPct = (player.ghostBleed / player.maxGhostBleed) * 100;
  const bioPct = (player.bioCalories / player.maxBioCalories) * 100;
  const xpPct = (player.xp / player.xpToNext) * 100;

  healthBarEl.style.width = `${Math.max(0, hpPct)}%`;
  ghostBarEl.style.width = `${Math.max(0, ghostPct)}%`;
  bioBarEl.style.width = `${Math.max(0, bioPct)}%`;
  xpBarEl.style.width = `${Math.max(0, xpPct)}%`;

  playerLevelEl.textContent = String(player.level);
  implantSlotsEl.textContent = `${player.implantSlots}/${player.implantSlots}`;
  creditsEl.textContent = String(player.credits);

  abilityListEl.innerHTML = abilityMeta
    .map((entry) => {
      const value = player.stats[entry.key];
      const canUpgrade = player.skillPoints > 0;
      return `
        <div class="ability-item">
          <div class="ability-info">
            <span class="ability-name">${entry.label}</span>
            <span class="ability-value">${value}</span>
          </div>
          <button data-stat="${entry.key}" ${canUpgrade ? '' : 'disabled'}>+1</button>
        </div>
      `;
    })
    .join('');

  questListEl.innerHTML = questState
    .map((quest) => {
      const done = quest.complete ? 'complete' : '';
      const progressText = quest.id === 'hold'
        ? `${Math.min(quest.progress, quest.target)}s / ${quest.target}s`
        : `${Math.min(quest.progress, quest.target)} / ${quest.target}`;
      return `
        <li class="quest-item ${done}">
          <div class="quest-title">
            <span>${quest.title}</span>
            <span>${quest.complete ? '✓' : '…'}</span>
          </div>
          <div class="quest-text">${quest.description}</div>
          <div class="quest-progress">${progressText}</div>
        </li>
      `;
    })
    .join('');
}

function levelUpIfNeeded() {
  while (player.xp >= player.xpToNext) {
    player.xp -= player.xpToNext;
    player.level += 1;
    player.skillPoints += 1;
    player.xpToNext = Math.round(player.xpToNext * 1.45);
    player.implantSlots = Math.min(8, 2 + Math.floor((player.level - 1) / 3));
  }
}

function triggerTacticalFocus() {
  if (!player.tacticalReady || player.tacticalFocus > 0) {
    return;
  }
  player.tacticalReady = false;
  player.tacticalFocus = 2.2;
  player.bioCalories = clamp(player.bioCalories - 8, 0, player.maxBioCalories);
  setTimeout(() => {
    player.tacticalReady = true;
  }, 1800);
}

function triggerBioBoost() {
  if (player.bioBoostTimer > 0) {
    return;
  }
  player.bioBoostTimer = 2.5;
  player.ghostBleed = clamp(player.ghostBleed + 12, 0, player.maxGhostBleed);
  player.bioCalories = clamp(player.bioCalories - 22, 0, player.maxBioCalories);
}

function handleAbilityUpgrade(statKey) {
  if (player.skillPoints <= 0) {
    return;
  }
  player.skillPoints -= 1;
  player.stats[statKey] += 1;
  updateHud();
}

function spawnInitialState() {
  spawnWave();
  addFuelCells();
  updateHud();
}

function updatePlayer(dt) {
  const moveX = (keys.ArrowRight || keys.d ? 1 : 0) - (keys.ArrowLeft || keys.a ? 1 : 0);
  const moveY = (keys.ArrowDown || keys.s ? 1 : 0) - (keys.ArrowUp || keys.w ? 1 : 0);
  const length = Math.hypot(moveX, moveY) || 1;

  const dx = pointer.x - player.x;
  const dy = pointer.y - player.y;
  player.facing = Math.atan2(dy, dx);

  const moveSpeed = player.speed * (1 + player.stats.tactics / 140) * (player.tacticalFocus > 0 ? 0.9 : 1);
  player.x += ((moveX / length) * moveSpeed * dt) || 0;
  player.y += ((moveY / length) * moveSpeed * dt) || 0;

  player.x = clamp(player.x, player.radius, WORLD.width - player.radius);
  player.y = clamp(player.y, player.radius, WORLD.height - player.radius);

  if (player.fireCooldown > 0) {
    player.fireCooldown -= dt;
  }

  if (player.tacticalFocus > 0) {
    player.tacticalFocus -= dt;
  }

  if (player.bioBoostTimer > 0) {
    player.bioBoostTimer -= dt;
  }

  if (pointer.down && player.fireCooldown <= 0) {
    const aimX = Math.cos(player.facing);
    const aimY = Math.sin(player.facing);
    const baseDamage = 18 + player.stats.ballistics * 1.7;
    const damage = player.bioBoostTimer > 0 ? baseDamage * 1.55 : baseDamage;
    bullets.push({
      x: player.x + aimX * 22,
      y: player.y + aimY * 22,
      vx: aimX * 660,
      vy: aimY * 660,
      life: 1.4,
      radius: 4,
      damage,
      owner: 'player'
    });
    player.fireCooldown = 0.16;
  }

  if (player.tacticalFocus <= 0) {
    player.tacticalFocus = 0;
  }

  player.bioCalories = clamp(player.bioCalories + dt * 3.8, 0, player.maxBioCalories);
  player.hp = clamp(player.hp + dt * 1.8, 0, player.maxHp);
}

function updateBullets(dt) {
  for (let i = bullets.length - 1; i >= 0; i -= 1) {
    const bullet = bullets[i];
    bullet.x += bullet.vx * dt;
    bullet.y += bullet.vy * dt;
    bullet.life -= dt;

    if (bullet.life <= 0 || bullet.x < -10 || bullet.x > WORLD.width + 10 || bullet.y < -10 || bullet.y > WORLD.height + 10) {
      bullets.splice(i, 1);
      continue;
    }

    if (bullet.owner === 'player') {
      for (let j = enemies.length - 1; j >= 0; j -= 1) {
        const enemy = enemies[j];
        if (dist(bullet.x, bullet.y, enemy.x, enemy.y) < enemy.radius + bullet.radius) {
          enemy.hp -= bullet.damage;
          bullets.splice(i, 1);
          if (enemy.hp <= 0) {
            enemies.splice(j, 1);
            player.xp += 14;
            player.credits += enemy.reward;
            raiderKills += enemy.kind === 'raider' ? 1 : 0;
            if (Math.random() < 0.2) {
              addPickup(enemy.x, enemy.y, 'scrap');
            }
          }
          break;
        }
      }
    } else {
      if (dist(bullet.x, bullet.y, player.x, player.y) < player.radius + bullet.radius) {
        const damage = bullet.damage * (1 + player.stats.grit / 120);
        player.hp -= damage;
        bullets.splice(i, 1);
      }
    }
  }
}

function updateEnemies(dt) {
  for (let i = enemies.length - 1; i >= 0; i -= 1) {
    const enemy = enemies[i];
    const dx = player.x - enemy.x;
    const dy = player.y - enemy.y;
    const distToPlayer = Math.hypot(dx, dy);

    if (distToPlayer > 0.1) {
      const angle = Math.atan2(dy, dx);
      const speed = enemy.speed * (player.tacticalFocus > 0 ? 0.75 : 1);
      enemy.x += Math.cos(angle) * speed * dt;
      enemy.y += Math.sin(angle) * speed * dt;
    }

    enemy.attackCooldown -= dt;
    if (distToPlayer < 220 && enemy.attackCooldown <= 0) {
      const aimX = Math.cos(Math.atan2(dy, dx));
      const aimY = Math.sin(Math.atan2(dy, dx));
      bullets.push({
        x: enemy.x + aimX * 18,
        y: enemy.y + aimY * 18,
        vx: aimX * 420,
        vy: aimY * 420,
        life: 1.2,
        radius: 4,
        damage: enemy.damage,
        owner: 'enemy'
      });
      enemy.attackCooldown = randomBetween(0.8, 1.6);
    }
  }
}

function updatePickups() {
  for (let i = pickups.length - 1; i >= 0; i -= 1) {
    const pickup = pickups[i];
    if (pickup.picked) {
      pickups.splice(i, 1);
      continue;
    }

    if (dist(pickup.x, pickup.y, player.x, player.y) < player.radius + pickup.radius + 8) {
      pickup.picked = true;
      if (pickup.kind === 'fuel') {
        questState[1].progress += 1;
        player.credits += 25;
        player.bioCalories = clamp(player.bioCalories + 25, 0, player.maxBioCalories);
      }
    }
  }
}

function updateQuestState(dt) {
  const raiderQuest = questState[0];
  const holdQuest = questState[2];

  raiderQuest.progress = raiderKills;
  if (raiderQuest.progress >= raiderQuest.target) {
    raiderQuest.complete = true;
  }

  if (questState[1].progress >= questState[1].target) {
    questState[1].complete = true;
  }

  if (!survivalActive && (raiderQuest.complete || questState[1].complete)) {
    survivalActive = true;
    waveTimer = 0;
    survivalSeconds = 0;
    spawnWave();
  }

  if (survivalActive) {
    waveTimer += dt;
    survivalSeconds += dt;
    holdQuest.progress = Math.min(Math.floor(survivalSeconds), holdQuest.target);
    if (survivalSeconds >= holdQuest.target) {
      holdQuest.complete = true;
      survivalActive = false;
      player.credits += 100;
      player.xp += 50;
    }

    if (waveTimer > 3.5) {
      waveTimer = 0;
      spawnEnemy(player.x + randomBetween(-220, 220), player.y + randomBetween(-220, 220), Math.random() < 0.25 ? 'brute' : 'raider');
    }
  }
}

function updateCamera() {
  camera.x = clamp(player.x - canvas.width / 2, 0, WORLD.width - canvas.width);
  camera.y = clamp(player.y - canvas.height / 2, 0, WORLD.height - canvas.height);
}

function drawBackground() {
  ctx.fillStyle = '#0b1117';
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  ctx.save();
  ctx.translate(-camera.x, -camera.y);

  ctx.fillStyle = '#171d24';
  ctx.fillRect(0, 0, WORLD.width, WORLD.height);

  for (let i = 0; i < 30; i += 1) {
    const x = (i % 6) * 280 + 120;
    const y = Math.floor(i / 6) * 270 + 110;
    ctx.fillStyle = 'rgba(123, 175, 218, 0.08)';
    ctx.fillRect(x, y, 200, 130);
  }

  ctx.strokeStyle = 'rgba(133, 170, 230, 0.18)';
  ctx.lineWidth = 2;
  for (let x = 0; x <= WORLD.width; x += 80) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, WORLD.height);
    ctx.stroke();
  }
  for (let y = 0; y <= WORLD.height; y += 80) {
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(WORLD.width, y);
    ctx.stroke();
  }

  for (const wall of walls) {
    ctx.fillStyle = '#2e3945';
    ctx.fillRect(wall.x, wall.y, wall.w, wall.h);
    ctx.strokeStyle = 'rgba(150, 210, 255, 0.22)';
    ctx.strokeRect(wall.x, wall.y, wall.w, wall.h);
  }

  for (const pickup of pickups) {
    if (pickup.picked) continue;
    ctx.fillStyle = pickup.kind === 'fuel' ? '#76f7ff' : '#ffce7a';
    ctx.beginPath();
    ctx.arc(pickup.x, pickup.y, pickup.radius, 0, Math.PI * 2);
    ctx.fill();
  }

  for (const enemy of enemies) {
    ctx.fillStyle = enemy.color;
    ctx.beginPath();
    ctx.arc(enemy.x, enemy.y, enemy.radius, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#0d1115';
    ctx.fillRect(enemy.x - 18, enemy.y - 26, 36, 5);
    ctx.fillStyle = '#7ef6b8';
    ctx.fillRect(enemy.x - 18, enemy.y - 26, (enemy.hp / enemy.maxHp) * 36, 5);
  }

  for (const bullet of bullets) {
    ctx.fillStyle = bullet.owner === 'player' ? '#d9faff' : '#ff9d9d';
    ctx.beginPath();
    ctx.arc(bullet.x, bullet.y, bullet.radius, 0, Math.PI * 2);
    ctx.fill();
  }

  ctx.fillStyle = '#59d9ff';
  ctx.beginPath();
  ctx.arc(player.x, player.y, player.radius, 0, Math.PI * 2);
  ctx.fill();

  ctx.strokeStyle = '#dffaff';
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(player.x, player.y);
  ctx.lineTo(player.x + Math.cos(player.facing) * 22, player.y + Math.sin(player.facing) * 22);
  ctx.stroke();

  ctx.restore();

  ctx.fillStyle = 'rgba(14, 20, 26, 0.5)';
  ctx.fillRect(18, canvas.height - 72, 200, 52);
  ctx.fillStyle = '#edf4ff';
  ctx.font = '13px Segoe UI';
  ctx.fillText('Tactical Focus', 28, canvas.height - 40);
  ctx.fillText(player.tacticalReady ? 'READY' : 'RECHARGING', 28, canvas.height - 20);
}

function drawCenterReticle() {
  const cx = canvas.width / 2;
  const cy = canvas.height / 2;
  ctx.save();
  ctx.strokeStyle = 'rgba(120, 220, 255, 0.8)';
  ctx.lineWidth = 1.2;
  ctx.beginPath();
  ctx.moveTo(cx - 10, cy);
  ctx.lineTo(cx + 10, cy);
  ctx.moveTo(cx, cy - 10);
  ctx.lineTo(cx, cy + 10);
  ctx.stroke();
  ctx.restore();
}

function render() {
  drawBackground();
  drawCenterReticle();

  ctx.fillStyle = 'rgba(9, 13, 18, 0.8)';
  ctx.fillRect(12, 12, 300, 64);
  ctx.fillStyle = '#edf4ff';
  ctx.font = 'bold 18px Segoe UI';
  ctx.fillText('Rusthollow // Bounty Terminal', 22, 38);
  ctx.font = '13px Segoe UI';
  ctx.fillStyle = '#a8b8d6';
  ctx.fillText(`Objective: ${questState.filter((quest) => !quest.complete).length} active`, 22, 58);
}

function update(dt) {
  const worldSpeed = player.tacticalFocus > 0 ? 0.36 : 1;
  updatePlayer(dt * worldSpeed);
  updateBullets(dt * worldSpeed);
  updateEnemies(dt * worldSpeed);
  updatePickups();
  updateQuestState(dt * worldSpeed);
  updateCamera();

  if (player.hp <= 0) {
    player.hp = player.maxHp;
    player.x = 180;
    player.y = 180;
    player.ghostBleed = clamp(player.ghostBleed + 12, 0, player.maxGhostBleed);
    player.credits = Math.max(0, player.credits - 15);
  }

  if (player.ghostBleed >= player.maxGhostBleed) {
    player.ghostBleed = player.maxGhostBleed;
    player.stats.bioSync += 1;
    player.ghostBleed = 25;
  }

  player.xp = clamp(player.xp, 0, 5000);
  levelUpIfNeeded();
  updateHud();
}

function loop(now) {
  const dt = Math.min((now - lastTime) / 1000, 0.033);
  lastTime = now;

  if (keys.Shift && player.tacticalReady) {
    triggerTacticalFocus();
  }

  if (keys.e || keys.E) {
    triggerBioBoost();
    keys.e = false;
    keys.E = false;
  }

  update(dt);
  render();
  requestAnimationFrame(loop);
}

function handlePointerMove(event) {
  const rect = canvas.getBoundingClientRect();
  const px = ((event.clientX - rect.left) / rect.width) * canvas.width;
  const py = ((event.clientY - rect.top) / rect.height) * canvas.height;
  pointer.x = px + camera.x;
  pointer.y = py + camera.y;
}

canvas.addEventListener('mousemove', handlePointerMove);
canvas.addEventListener('mousedown', () => {
  pointer.down = true;
});
window.addEventListener('mouseup', () => {
  pointer.down = false;
});
window.addEventListener('keydown', (event) => {
  keys[event.key] = true;
  if (event.key === ' ') {
    event.preventDefault();
    triggerTacticalFocus();
  }
});
window.addEventListener('keyup', (event) => {
  keys[event.key] = false;
});

abilityListEl.addEventListener('click', (event) => {
  const target = event.target.closest('button[data-stat]');
  if (!target) return;
  handleAbilityUpgrade(target.dataset.stat);
});

window.addEventListener('pointerleave', () => {
  pointer.down = false;
});

pointer.x = player.x;
pointer.y = player.y;
spawnInitialState();
requestAnimationFrame(loop);
