import { exec } from 'child_process';
import { promisify } from 'util';
const execAsync = promisify(exec);
export async function runBuild(projectRoot) {
    try {
        // We run with -clp:NoSummary to reduce noise, or we can parse the full output
        const { stdout, stderr } = await execAsync('dotnet build', { cwd: projectRoot });
        return {
            success: true,
            output: stdout
        };
    }
    catch (error) {
        // dotnet build returns non-zero on compilation errors
        return {
            success: false,
            output: error.stdout || error.message,
            error: error.stderr
        };
    }
}
export function parseBuildErrors(output) {
    const lines = output.split('\n');
    const errors = lines.filter(line => line.includes(': error '));
    const warnings = lines.filter(line => line.includes(': warning '));
    return {
        errors: errors.map(e => e.trim()),
        warnings: warnings.map(w => w.trim())
    };
}
